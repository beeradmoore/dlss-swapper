import xml.etree.ElementTree as ET
import os
import argparse


def parse_resw(file_path):
    data = {}
    if not os.path.exists(file_path):
        return data
    try:
        tree = ET.parse(file_path)
        root = tree.getroot()
        for data_elem in root.findall("data"):
            name = data_elem.get("name")
            data[name] = True
    except Exception as e:
        print(f"Error parsing {file_path}: {e}")
    return data


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", default=os.path.dirname(__file__) or ".")
    parser.add_argument("--source", default="en-US")
    args = parser.parse_args()
    base_path = args.base
    source_code = args.source
    en_path = os.path.join(base_path, source_code, "Resources.resw")
    en_data = parse_resw(en_path)
    langs = []
    for name in os.listdir(base_path):
        dir_path = os.path.join(base_path, name)
        if not os.path.isdir(dir_path):
            continue
        resw_path = os.path.join(dir_path, "Resources.resw")
        if os.path.exists(resw_path):
            langs.append((name, resw_path))
    for lang_code, resw_path in sorted(langs):
        if lang_code == source_code:
            continue
        lang_data = parse_resw(resw_path)
        missing_keys = [k for k in en_data.keys() if k not in lang_data]
        print(f"Language: {lang_code}")
        print(f"Missing keys: {len(missing_keys)}")
        if missing_keys:
            for k in missing_keys:
                print(f"  - {k}")
        print("-" * 40)
    print("Summary of missing keys per language:")
    for lang_code, resw_path in sorted(langs):
        if lang_code == source_code:
            continue
        lang_data = parse_resw(resw_path)
        missing_keys = [k for k in en_data.keys() if k not in lang_data]
        print(f"{lang_code}: {len(missing_keys)}")


if __name__ == "__main__":
    main()
