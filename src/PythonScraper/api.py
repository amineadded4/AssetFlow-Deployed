from flask import Flask, request, jsonify
from flask_cors import CORS
import sys
import os

sys.path.append(os.path.dirname(__file__))
from price_scraper import scraper_prix

app = Flask(__name__)
CORS(app)


def nettoyer_query(query: str) -> str:
    """Supprime les caractères qui cassent les URLs."""
    return (
        query
        .replace('"', '')
        .replace("'", '')
        .replace('`', '')
        .strip()
    )


@app.route('/scrape', methods=['GET'])
def scrape():
    query_brute = request.args.get('q')
    if not query_brute:
        return jsonify({'error': 'Paramètre "q" manquant'}), 400

    query = nettoyer_query(query_brute)
    print(f"[API] Recherche : '{query}'")

    try:
        resultat = scraper_prix(query)
        return jsonify(resultat)
    except Exception as e:
        return jsonify({'error': str(e)}), 500


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=10000, debug=False)