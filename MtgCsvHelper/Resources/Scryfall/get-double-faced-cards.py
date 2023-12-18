# Small script to download scryfall data for names of double-faced cards

import json
import requests

def filter_json(data, output_file):
    data['data'] = [line for line in data['data'] if " // " in line]

    with open(output_file, 'w') as f:
        json.dump(data, f, indent=4)

if __name__ == "__main__":
    json_data = requests.get("https://api.scryfall.com/catalog/card-names").json()
    output_file = "scryfall-double-faced-cards.json"
    filter_json(json_data, output_file)
    print("Filtered data written to", output_file)
