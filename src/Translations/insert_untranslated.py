import os
import xml.etree.ElementTree as ET
import re


BASE_PATH = os.path.dirname(__file__) or "."
SOURCE_CODE = "en-US"
UNTRANSLATED_PREFIX = "__UNTRANSLATED__"


def parse_values(path):
    data = {}
    try:
        tree = ET.parse(path)
        root = tree.getroot()
        for d in root.findall("data"):
            name = d.get("name")
            v = d.findtext("value") or ""
            c = d.findtext("comment")
            data[name] = {"value": v, "comment": c}
    except Exception as e:
        print(f"Error parsing {path}: {e}")
    return data


def parse_keys(path):
    keys = set()
    try:
        tree = ET.parse(path)
        root = tree.getroot()
        for d in root.findall("data"):
            name = d.get("name")
            keys.add(name)
    except Exception as e:
        print(f"Error parsing {path}: {e}")
    return keys


def detect_encoding_and_newline(path):
    with open(path, "rb") as f:
        raw = f.read()
    encoding = "utf-8"
    if raw.startswith(b"\xef\xbb\xbf"):
        encoding = "utf-8-sig"
    elif raw.startswith(b"\xff\xfe") or raw.startswith(b"\xfe\xff"):
        encoding = "utf-16"
    try:
        text = raw.decode(encoding)
    except Exception:
        encoding = "cp1252"
        text = raw.decode(encoding, errors="replace")
    nl = "\n"
    if "\r\n" in text:
        nl = "\r\n"
    elif "\r" in text and "\n" not in text:
        nl = "\r"
    return encoding, nl, text


def xml_escape(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def get_en_order(path):
    order = []
    try:
        tree = ET.parse(path)
        root = tree.getroot()
        for d in root.findall("data"):
            name = d.get("name")
            order.append(name)
    except Exception as e:
        print(f"Error parsing order from {path}: {e}")
    return order


def find_data_blocks(text):
    blocks = []
    pattern = re.compile(r'(<data\s+name="([^"]+)"[^>]*>[\s\S]*?</data>)', re.MULTILINE)
    for m in pattern.finditer(text):
        full = m.group(1)
        name = m.group(2)
        start = m.start(1)
        end = m.end(1)
        blocks.append((name, start, end, full))
    return blocks


def plan_insert_position(en_order, target_blocks_map, missing_key):
    try:
        idx = en_order.index(missing_key)
    except ValueError:
        return None
    for j in range(idx + 1, len(en_order)):
        next_key = en_order[j]
        if next_key in target_blocks_map:
            return ("before", target_blocks_map[next_key][0])
    for j in range(idx - 1, -1, -1):
        prev_key = en_order[j]
        if prev_key in target_blocks_map:
            return ("after", target_blocks_map[prev_key][1])
    return None


def insert_missing_values():
    source_path = os.path.join(BASE_PATH, SOURCE_CODE, "Resources.resw")
    en = parse_values(source_path)
    en_order = get_en_order(source_path)
    langs = []
    for name in os.listdir(BASE_PATH):
        d = os.path.join(BASE_PATH, name)
        if not os.path.isdir(d):
            continue
        p = os.path.join(d, "Resources.resw")
        if os.path.exists(p):
            langs.append((name, p))
    total_added = 0
    for code, path in sorted(langs):
        if code == SOURCE_CODE:
            continue
        existing = parse_keys(path)
        missing = [k for k in en.keys() if k not in existing]
        if not missing:
            continue
        enc, nl, text = detect_encoding_and_newline(path)
        target_blocks = find_data_blocks(text)
        target_map = {n: (s, e, full) for (n, s, e, full) in target_blocks}
        inserts = []
        for k in missing:
            val = en[k]["value"]
            prefix = "" if code.startswith("en-") else UNTRANSLATED_PREFIX
            esc_val = xml_escape(f"{prefix}{val}")
            block = (
                f'  <data name="{k}" xml:space="preserve">{nl}'
                f"    <value>{esc_val}</value>{nl}"
                f"  </data>{nl}"
            )
            pos_plan = plan_insert_position(en_order, target_map, k)
            if pos_plan is None:
                closing_idx = text.rfind(f"{nl}</root>")
                if closing_idx == -1:
                    closing_idx = len(text)
                inserts.append((closing_idx, block))
            else:
                where, pos = pos_plan
                if where == "before":
                    inserts.append((pos, block))
                else:
                    inserts.append((pos + len(nl), block))
        inserts.sort(key=lambda x: x[0])
        new_text = text
        offset = 0
        for pos, block in inserts:
            insert_at = pos + offset
            new_text = new_text[:insert_at] + block + new_text[insert_at:]
            offset += len(block)
        with open(path, "w", encoding=enc, newline="") as f:
            f.write(new_text)
        print(f"{code}: added {len(missing)} strings")
        total_added += len(missing)
    print(f"Total added: {total_added}")


if __name__ == "__main__":
    insert_missing_values()
