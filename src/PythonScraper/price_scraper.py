#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import json
import os
from datetime import datetime
import requests
from bs4 import BeautifulSoup

HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) "
        "Chrome/120.0.0.0 Safari/537.36"
    ),
    "Accept-Language": "fr-FR,fr;q=0.9",
}

# =============================================================================
# CHARGEMENT DES CATÉGORIES
# =============================================================================

def charger_categories(fichier: str, site: str) -> dict:
    json_path = os.path.join(os.path.dirname(__file__), fichier)
    try:
        with open(json_path, "r", encoding="utf-8") as f:
            data = json.load(f)
        plat = {}
        for section in data.values():
            plat.update(section)
        print(f"[{site}] ✅ {len(plat)} catégories chargées")
        return plat
    except FileNotFoundError:
        print(f"[{site}] ❌ {fichier} introuvable")
        return {}
    except json.JSONDecodeError as e:
        print(f"[{site}] ❌ Erreur JSON : {e}")
        return {}


MYTEK_CATEGORIES      = charger_categories("mytek_categories.json",      "MyTek")
SPACENET_CATEGORIES   = charger_categories("spacenet_categories.json",   "Spacenet")
TUNISIANET_CATEGORIES = charger_categories("tunisianet_categories.json", "Tunisianet")


def trouver_url_categorie(query: str, categories: dict) -> str | None:
    q = query.lower().strip()
    if q in categories:
        return categories[q]
    for cle, url in categories.items():
        if cle in q:
            return url
    for cle, url in categories.items():
        if q in cle:
            return url
    return None


def get_soup(url: str) -> BeautifulSoup | None:
    try:
        r = requests.get(url, headers=HEADERS, timeout=20)
        r.raise_for_status()
        return BeautifulSoup(r.text, "html.parser")
    except Exception as e:
        print(f"[HTTP] Erreur sur {url} : {e}")
        return None


# =============================================================================
# SCRAPING MYTEK
# =============================================================================

def scraper_mytek(nom_article: str) -> list:
    headers_api = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Accept": "application/json",
        "X-Requested-With": "XMLHttpRequest"
    }

    # Extraire category_id depuis l'URL catégorie si disponible
    url_categorie = trouver_url_categorie(nom_article, MYTEK_CATEGORIES)
    category_id = None
    if url_categorie:
        import re
        match = re.search(r'category_ids%5D=(\d+)', url_categorie)
        if match:
            category_id = match.group(1)
            print(f"\n[MyTek] ✅ Catégorie ID={category_id} → {url_categorie}")
        else:
            print(f"\n[MyTek] ✅ Catégorie trouvée mais sans ID → fallback nom")
    else:
        print(f"\n[MyTek] ⚠ Pas de catégorie → recherche par nom")

    # Construire l'URL API selon ce qu'on a
    if category_id:
        search_url = (
            "https://www.mytek.tn/rest/default/V1/products"
            "?searchCriteria[filterGroups][0][filters][0][field]=category_id"
            f"&searchCriteria[filterGroups][0][filters][0][value]={category_id}"
            "&searchCriteria[filterGroups][0][filters][0][conditionType]=eq"
            "&searchCriteria[filterGroups][1][filters][0][field]=status"
            "&searchCriteria[filterGroups][1][filters][0][value]=1"
            "&searchCriteria[filterGroups][1][filters][0][conditionType]=eq"
            "&searchCriteria[pageSize]=48"
            "&fields=items[id,name,price,status]"
        )
    else:
        search_url = (
            "https://www.mytek.tn/rest/default/V1/products"
            "?searchCriteria[filterGroups][0][filters][0][field]=name"
            f"&searchCriteria[filterGroups][0][filters][0][value]=%25{nom_article.replace(' ', '%20')}%25"
            "&searchCriteria[filterGroups][0][filters][0][conditionType]=like"
            "&searchCriteria[filterGroups][1][filters][0][field]=status"
            "&searchCriteria[filterGroups][1][filters][0][value]=1"
            "&searchCriteria[filterGroups][1][filters][0][conditionType]=eq"
            "&searchCriteria[pageSize]=48"
            "&fields=items[id,name,price,status]"
        )

    try:
        r = requests.get(search_url, headers=headers_api, timeout=20)
        r.raise_for_status()
        data = r.json()
    except Exception as e:
        print(f"[MyTek] ❌ Erreur API REST : {e}")
        return []

    items_rest = data.get("items", [])
    print(f"[MyTek] {len(items_rest)} produit(s) depuis API REST")
    if not items_rest:
        return []

    # API volatile pour prix final + stock
    ids = [str(item["id"]) for item in items_rest if "id" in item]
    noms = {str(item["id"]): item.get("name", nom_article) for item in items_rest}

    api_url = f"https://www.mytek.tn/api/products/volatile?ids={','.join(ids)}"
    try:
        r2 = requests.get(api_url, headers=headers_api, timeout=20)
        r2.raise_for_status()
        volatile_data = r2.json()
    except Exception as e:
        print(f"[MyTek] ⚠ API volatile échouée : {e}")
        volatile_data = {"items": []}

    volatile_map = {}
    for v in volatile_data.get("items", []):
        pid = str(v.get("id", ""))
        volatile_map[pid] = {
            "prix":  v.get("final_price") or v.get("price"),
            "stock": v.get("erpstock", {}).get("label", "Non indiqué") if isinstance(v.get("erpstock"), dict) else "Non indiqué"
        }

    # Filtre par mots-clés si pas de category_id (fallback nom)
    mots = [m for m in nom_article.lower().split() if len(m) > 2]

    resultats = []
    for item in items_rest:
        pid  = str(item.get("id", ""))
        nom  = noms.get(pid, nom_article)
        lien = f"https://www.mytek.tn/catalog/product/view/id/{pid}"

        # Filtrer par mots-clés uniquement si recherche par nom (pas catégorie)
        if not category_id and mots:
            if not any(m in nom.lower() for m in mots):
                continue

        if pid in volatile_map and volatile_map[pid]["prix"]:
            prix  = volatile_map[pid]["prix"]
            stock = volatile_map[pid]["stock"]
        else:
            prix  = item.get("price")
            stock = "Non indiqué"

        if not prix:
            continue

        resultats.append({
            "site":          "MyTek",
            "nom_produit":   nom,
            "prix":          round(float(prix), 3),
            "devise":        "TND",
            "stock":         stock,
            "url":           lien,
            "date_scraping": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        })

    print(f"[MyTek] ✅ {len(resultats)} résultat(s)")
    return resultats


# =============================================================================
# SCRAPING TUNISIANET
# =============================================================================

def scraper_tunisianet(nom_article: str) -> list:
    url_categorie = trouver_url_categorie(nom_article, TUNISIANET_CATEGORIES)
    if url_categorie:
        url = url_categorie
        par_categorie = True
        print(f"\n[Tunisianet] ✅ Catégorie → {url}")
    else:
        url = (
            f"https://www.tunisianet.com.tn/recherche?controller=search"
            f"&orderby=price&orderway=asc&s={nom_article.replace(' ', '+')}&submit_search="
        )
        par_categorie = False
        print(f"\n[Tunisianet] ⚠ Fallback → {url}")

    soup = get_soup(url)
    if not soup:
        return []

    produits = soup.select("article.product-miniature")
    print(f"[Tunisianet] {len(produits)} produit(s)")

    resultats = []
    mots = [m for m in nom_article.lower().split() if len(m) > 3]

    for produit in produits:
        lien_el = produit.select_one("h2.product-title a")
        nom  = lien_el.text.strip() if lien_el else nom_article
        lien = lien_el.get("href", url) if lien_el else url

        if par_categorie and len(mots) >= 2:
            if not any(m in nom.lower() for m in mots):
                continue

        # PRIX
        prix = None
        prix_el = produit.select_one("span[itemprop='price']")
        if prix_el:
            content = prix_el.get("content")
            if content:
                try:
                    prix = float(content)
                except:
                    pass
            if prix is None:
                try:
                    txt = prix_el.text.replace("DT","").replace("TND","").replace("\xa0","").replace("\u202f","").replace(" ","").strip()
                    txt = txt.replace(",",".")
                    parties = txt.split(".")
                    if len(parties) > 2:
                        txt = "".join(parties[:-1]) + "." + parties[-1]
                    prix = float(txt)
                except:
                    pass
        if prix is None:
            continue

        # STOCK
        stock = "Non indiqué"
        stock_el = produit.select_one("div.product-availability span")
        if stock_el:
            stock = stock_el.text.strip()

        resultats.append({
            "site": "Tunisianet",
            "nom_produit": nom,
            "prix": round(prix, 3),
            "devise": "TND",
            "stock": stock,
            "url": lien,
            "date_scraping": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        })

    return resultats


# =============================================================================
# SCRAPING SPACENET
# =============================================================================

def scraper_spacenet(nom_article: str) -> list:
    url_categorie = trouver_url_categorie(nom_article, SPACENET_CATEGORIES)
    if url_categorie:
        url = url_categorie
        par_categorie = True
        print(f"\n[Spacenet] ✅ Catégorie → {url}")
    else:
        url = (
            f"https://spacenet.tn/recherche?controller=search"
            f"&orderby=position&orderway=desc&search_query={nom_article.replace(' ', '+')}&submit_search="
        )
        par_categorie = False
        print(f"\n[Spacenet] ⚠ Fallback → {url}")

    soup = get_soup(url)
    if not soup:
        return []

    produits = soup.select("div#box-product-grid div.field-product-item.product-miniature")
    if not produits:
        produits = soup.select("div.field-product-item.product-miniature")
    print(f"[Spacenet] {len(produits)} produit(s)")

    resultats = []
    mots = [m for m in nom_article.lower().split() if len(m) > 3]

    for produit in produits:
        lien_el = produit.select_one("h2.product_name a")
        nom  = lien_el.text.strip() if lien_el else nom_article
        lien = lien_el.get("href", url) if lien_el else url

        if par_categorie and len(mots) >= 2:
            if not any(m in nom.lower() for m in mots):
                continue

        # PRIX
        prix = None
        for prix_el in produit.select("span.price"):
            txt = prix_el.text.strip()
            if txt and any(c.isdigit() for c in txt):
                try:
                    prix_clean = (
                        txt
                        .replace("DT","").replace("TND","")
                        .replace("\u202f","").replace("\u00a0","")
                        .replace("\u2009","").replace("\xa0","")
                        .replace(" ","").replace("\t","").strip()
                    )
                    if "," in prix_clean:
                        prix_clean = prix_clean.replace(".", "").replace(",", ".")
                    prix = float(prix_clean)
                    break
                except:
                    continue
        if prix is None:
            continue

        # STOCK
        stock = "Non indiqué"
        qty_div = produit.select_one("div.product-quantities")
        if qty_div:
            for lbl in qty_div.select("label"):
                txt = lbl.text.strip()
                if txt:
                    stock = txt
                    break

        resultats.append({
            "site": "Spacenet",
            "nom_produit": nom,
            "prix": round(prix, 3),
            "devise": "TND",
            "stock": stock,
            "url": lien,
            "date_scraping": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        })

    return resultats


# =============================================================================
# FONCTION PRINCIPALE
# =============================================================================

def scraper_prix(nom_article: str) -> dict:
    tous_resultats = []

    res_mytek = scraper_mytek(nom_article)
    tous_resultats.extend(res_mytek)
    print(f"[MyTek] {len(res_mytek)} résultat(s)")

    res_tunisianet = scraper_tunisianet(nom_article)
    tous_resultats.extend(res_tunisianet)
    print(f"[Tunisianet] {len(res_tunisianet)} résultat(s)")

    res_spacenet = scraper_spacenet(nom_article)
    tous_resultats.extend(res_spacenet)
    print(f"[Spacenet] {len(res_spacenet)} résultat(s)")

    if tous_resultats:
        meilleur = min(tous_resultats, key=lambda x: x["prix"])
        return {
            "succes": True,
            "article": nom_article,
            "date_recherche": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
            "nombre_resultats": len(tous_resultats),
            "resultats": tous_resultats,
            "meilleur_prix": meilleur,
            "recommandation": {
                "site": meilleur["site"],
                "prix": meilleur["prix"],
                "url": meilleur["url"],
                "message": f"Meilleur prix trouvé sur {meilleur['site']}"
            }
        }

    return {
        "succes": False,
        "article": nom_article,
        "date_recherche": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "nombre_resultats": 0,
        "resultats": [],
        "meilleur_prix": None,
        "recommandation": {
            "site": None, "prix": None, "url": None,
            "message": f"Aucun résultat trouvé pour '{nom_article}'"
        }
    }