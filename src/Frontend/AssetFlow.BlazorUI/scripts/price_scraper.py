#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Script de Web Scraping MyTek + Tunisianet (Selenium)
Auteur: STAMBOULI Nada
Projet: GestionStock SmartFuture
"""

import sys
import json
import time
from datetime import datetime

from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.common.by import By
from webdriver_manager.chrome import ChromeDriverManager


# =============================================================================
# CRÉATION DU DRIVER (réutilisé pour les deux sites)
# =============================================================================

def creer_driver():
    options = webdriver.ChromeOptions()
    options.add_argument("--headless")
    options.add_argument("--window-size=1920,1080")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")
    return webdriver.Chrome(
        service=Service(ChromeDriverManager().install()),
        options=options
    )


# =============================================================================
# SCRAPING MYTEK
# =============================================================================

def scraper_mytek(nom_article, driver):
    url = (
        "https://www.mytek.tn/myteksearch/index/productsearch/?q="
        + nom_article.replace(" ", "+")
    )
    print(f"\n[MyTek] URL : {url}")

    driver.get(url)
    time.sleep(3)

    produits = driver.find_elements(By.CSS_SELECTOR, "div.product-container")
    print(f"[MyTek] {len(produits)} produit(s) trouvé(s)")

    if not produits:
        return []

    resultats = []
    for produit in produits:

        # NOM
        try:
            nom = produit.find_element(By.CSS_SELECTOR, "a.product-item-link").text.strip()
        except:
            nom = nom_article

        # PRIX
        try:
            prix_txt = produit.find_element(By.CSS_SELECTOR, "span.final-price").text
            prix = float(
                prix_txt.replace("DT", "").replace("TND", "")
                        .replace(",", ".").replace(" ", "").replace("\xa0", "")
            )
        except:
            continue

        # STOCK
        try:
            classes = produit.find_element(By.CSS_SELECTOR, "div.stock").get_attribute("class")
            if "availables"     in classes: stock = "En stock"
            elif "incoming"     in classes: stock = "En arrivage"
            elif "out-of-stock" in classes: stock = "Épuisé"
            else:                           stock = "État inconnu"
        except:
            stock = "Non indiqué"

        # LIEN
        try:
            lien = produit.find_element(By.CSS_SELECTOR, "a.product-item-link").get_attribute("href")
        except:
            lien = url

        resultats.append({
            "site":          "MyTek",
            "nom_produit":   nom,
            "prix":          round(prix, 3),
            "devise":        "TND",
            "stock":         stock,
            "url":           lien,
            "date_scraping": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        })

    return resultats


# =============================================================================
# SCRAPING TUNISIANET
# =============================================================================

def scraper_tunisianet(nom_article, driver):
    url = (
        "https://www.tunisianet.com.tn/recherche?controller=search"
        "&orderby=price&orderway=asc&s="
        + nom_article.replace(" ", "+")
        + "&submit_search="
    )
    print(f"\n[Tunisianet] URL : {url}")

    driver.get(url)
    time.sleep(3)

    produits = driver.find_elements(By.CSS_SELECTOR, "article.product-miniature")
    print(f"[Tunisianet] {len(produits)} produit(s) trouvé(s)")

    if not produits:
        return []

    resultats = []
    for produit in produits:

        # NOM + LIEN
        try:
            lien_el = produit.find_element(By.CSS_SELECTOR, "h2.product-title a")
            nom  = lien_el.text.strip()
            lien = lien_el.get_attribute("href")
        except:
            nom  = nom_article
            lien = url

        # PRIX — chercher directement dans l'article, prendre le PREMIER span[itemprop='price']
        # 2 spans de prix → on prend le premier non vide.
        try:
            prix_els = produit.find_elements(By.CSS_SELECTOR, "span[itemprop='price']")
            if not prix_els:
                # fallback : span.price
                prix_els = produit.find_elements(By.CSS_SELECTOR, "span.price")
            
            prix_txt = ""
            for el in prix_els:
                txt = el.text.strip()
                if txt and ("DT" in txt or any(c.isdigit() for c in txt)):
                    prix_txt = txt
                    break
            
            if not prix_txt:
                # Essayer l'attribut 'content' (microdata)
                for el in prix_els:
                    content = el.get_attribute("content")
                    if content:
                        prix_txt = content
                        break

            prix = float(
                prix_txt.replace("DT", "").replace("TND", "")
                        .replace(",", ".").replace(" ", "").replace("\xa0", "").strip()
            )
        except Exception as e:
            print(f"  [Tunisianet] ⚠ Prix introuvable pour : {nom[:50]} ({e})")
            continue

        # STOCK
        stock = "Non indiqué"
        try:
            spans = produit.find_elements(By.CSS_SELECTOR, "div#stock_availability span")
            for span in spans:
                txt = span.text.strip()
                if txt:  # prendre le premier span non vide
                    stock = txt
                    break
        except:
            pass

        resultats.append({
            "site":          "Tunisianet",
            "nom_produit":   nom,
            "prix":          round(prix, 3),
            "devise":        "TND",
            "stock":         stock,
            "url":           lien,
            "date_scraping": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        })

    return resultats

# =============================================================================
# SCRAPING SPACENET
# =============================================================================
def scraper_spacenet(nom_article, driver):
    url = (
        "https://spacenet.tn/recherche?controller=search"
        "&orderby=position&orderway=desc&search_query="
        + nom_article.replace(" ", "+")
        + "&submit_search="
    )
    print(f"\n[Spacenet] URL : {url}")

    driver.get(url)
    time.sleep(3)

    produits = driver.find_elements(By.CSS_SELECTOR, "div.field-product-item.product-miniature")
    print(f"[Spacenet] {len(produits)} produit(s) trouvé(s)")

    if not produits:
        return []

    resultats = []
    for produit in produits:

        # NOM + LIEN
        try:
            lien_el = produit.find_element(By.CSS_SELECTOR, "h2.product_name a")
            nom  = lien_el.text.strip()
            lien = lien_el.get_attribute("href")
        except:
            nom  = nom_article
            lien = url

        # PRIX
        try:
            prix_els = produit.find_elements(By.CSS_SELECTOR, "span.price")
            prix_txt = ""
            for el in prix_els:
                txt = el.text.strip()
                if txt and any(c.isdigit() for c in txt):
                    prix_txt = txt
                    break
            prix = float(
                prix_txt.replace("DT", "").replace("TND", "")
                        .replace(",", ".").replace("\xa0", "").replace(" ", "").strip()
            )
        except Exception as e:
            print(f"  [Spacenet] ⚠ Prix introuvable pour : {nom[:50]} ({e})")
            continue

        # STOCK
        stock = "Non indiqué"
        try:
            label = produit.find_element(By.CSS_SELECTOR, "label.label-available")
            stock = label.text.strip()
        except:
            pass

        resultats.append({
            "site":          "Spacenet",
            "nom_produit":   nom,
            "prix":          round(prix, 3),
            "devise":        "TND",
            "stock":         stock,
            "url":           lien,
            "date_scraping": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        })

    return resultats

# =============================================================================
# FONCTION PRINCIPALE
# =============================================================================

def scraper_prix(nom_article):
    driver = creer_driver()
    tous_resultats = []

    try:
        res_mytek = scraper_mytek(nom_article, driver)
        tous_resultats.extend(res_mytek)
        print(f"[MyTek] {len(res_mytek)} résultat(s) récupéré(s)")

        res_tunisianet = scraper_tunisianet(nom_article, driver)
        tous_resultats.extend(res_tunisianet)
        print(f"[Tunisianet] {len(res_tunisianet)} résultat(s) récupéré(s)")

        res_spacenet = scraper_spacenet(nom_article, driver)
        tous_resultats.extend(res_spacenet)
        print(f"[Spacenet] {len(res_spacenet)} résultat(s) récupéré(s)")

    finally:
        driver.quit()

    if tous_resultats:
        meilleur = min(tous_resultats, key=lambda x: x["prix"])
        return {
            "succes":           True,
            "article":          nom_article,
            "date_recherche":   datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
            "nombre_sites":     2,
            "nombre_resultats": len(tous_resultats),
            "resultats":        tous_resultats,
            "meilleur_prix":    meilleur,
            "recommandation": {
                "site":    meilleur["site"],
                "prix":    meilleur["prix"],
                "url":     meilleur["url"],
                "message": f"Meilleur prix trouvé sur {meilleur['site']}"
            }
        }

    return {
        "succes":           False,
        "article":          nom_article,
        "date_recherche":   datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "nombre_resultats": 0,
        "resultats":        [],
        "meilleur_prix":    None,
        "recommandation": {
            "site": None, "prix": None, "url": None,
            "message": "Aucun résultat trouvé sur MyTek et Tunisianet"
        }
    }


# =============================================================================
# AFFICHAGE CONSOLE
# =============================================================================

def afficher_resultats(reponse):
    print("\n" + "=" * 55)
    print("           RÉSULTATS DU SCRAPING")
    print("=" * 55)

    if not reponse["succes"]:
        print("❌ Aucun résultat trouvé")
        return

    print(f"Article       : {reponse['article']}")
    print(f"Nb résultats  : {reponse['nombre_resultats']}")
    print(f"Sites scrapés : {reponse['nombre_sites']} (MyTek + Tunisianet + Spacenet)")    
    print()

    # Grouper par site
    sites = {}
    for r in reponse["resultats"]:
        sites.setdefault(r["site"], []).append(r)

    for site, produits in sites.items():
        print(f"── {site} ({len(produits)} produit(s)) ──")
        for i, r in enumerate(produits, 1):
            print(f"  [{i}] {r['nom_produit'][:60]}")
            print(f"       Prix  : {r['prix']} TND")
            print(f"       Stock : {r['stock']}")
            print(f"       URL   : {r['url']}")
        print()

    best = reponse["meilleur_prix"]
    print("=" * 55)
    print("🏆 MEILLEUR PRIX")
    print(f"   Site    : {best['site']}")
    print(f"   Produit : {best['nom_produit'][:60]}")
    print(f"   Prix    : {best['prix']} TND")
    print(f"   Stock   : {best['stock']}")
    print(f"   URL     : {best['url']}")
    print("=" * 55)


# =============================================================================
# MAIN
# =============================================================================

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python price_scraper.py <nom_article>")
        print("Exemple: python price_scraper.py souris")
        sys.exit(1)

    nom_article = " ".join(sys.argv[1:])
    print(f"\n🔍 Recherche : '{nom_article}'")

    reponse = scraper_prix(nom_article)
    afficher_resultats(reponse)

    print("\n📦 JSON COMPLET :\n")
    print(json.dumps(reponse, indent=2, ensure_ascii=False))