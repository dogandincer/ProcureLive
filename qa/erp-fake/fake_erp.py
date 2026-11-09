# -*- coding: utf-8 -*-
"""
Sahte UyumERP SOAP sunucusu (yalnızca test için). Gerçek ERP'ye dokunmadan ProcureLive'ın ERP entegrasyonunu uçtan uca dener.
UyumGetWebService.asmx: GetDataTableWithSQL (FIND_ENTITY, INVD_ITEM, PSMT_OFFER_M/D ve kimlik sorguları, COUNT).
UyumSaveWebService.asmx: SaveEntity, SaveItem, SaveOffer, SaveOrderM, SaveUyumObjectFromXML — kayıtları bellekte tutar.
GET /_requests: alınan tüm istekler (JSON), POST /_reset: belleği sıfırla.
Kimlik: kullanıcı "test.user", şifre "test.pass"; başka bilgiyle Result=false döner.
Kullanım: python qa/erp-fake/fake_erp.py [port=8099]
"""
import json
import re
import sys
import xml.etree.ElementTree as ET
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from xml.sax.saxutils import escape

TEM = "{http://tempuri.org/}"
USER, PASSWORD = "test.user", "test.pass"

ENTITIES = [
    {"ENTITY_ID": 501, "ENTITY_CODE": "320.01.001", "ENTITY_NAME": "Marmara Kablo Sanayi A.Ş.", "TAX_NO": "6110000001", "TEL1": "0216 555 10 01", "EMAIL": "satis@marmarakablo.example"},
    {"ENTITY_ID": 502, "ENTITY_CODE": "320.01.002", "ENTITY_NAME": "Anadolu Pano Ltd. Şti.", "TAX_NO": "6110000002", "TEL1": "0312 555 10 02", "EMAIL": "teklif@anadolupano.example"},
    {"ENTITY_ID": 503, "ENTITY_CODE": "320.01.003", "ENTITY_NAME": "Ege Bilişim Teknolojileri A.Ş.", "TAX_NO": None, "TEL1": None, "EMAIL": None},
    {"ENTITY_ID": 601, "ENTITY_CODE": "120.01.001", "ENTITY_NAME": "Bir Müşteri A.Ş.", "TAX_NO": "6220000001", "TEL1": None, "EMAIL": None},
]
ITEMS = [
    {"ITEM_ID": 9001, "ITEM_CODE": "KBL-NYY-416", "ITEM_NAME": "NYY 4x16 mm² Enerji Kablosu", "UNIT_CODE": "MT"},
    {"ITEM_ID": 9002, "ITEM_CODE": "PNO-AG-001", "ITEM_NAME": "AG Dağıtım Panosu", "UNIT_CODE": "ADET"},
    {"ITEM_ID": 9003, "ITEM_CODE": "TRF-1000-KT", "ITEM_NAME": "1000 kVA Kuru Tip Trafo", "UNIT_CODE": "ADET"},
]
state = {"requests": [], "offers": {}, "orders": {}, "next_offer_m": 70000, "next_offer_d": 80000, "revisions": []}


def table(rows):
    body = "".join(
        "<Table1>" + "".join(f"<{k}>{escape(str(v))}</{k}>" for k, v in r.items() if v is not None) + "</Table1>" for r in rows)
    return ('<GetDataTableWithSQLResult xmlns="http://tempuri.org/"><Result>true</Result><Message/>'
            '<Value><xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" id="NewDataSet"/>'
            '<diffgr:diffgram xmlns:diffgr="urn:schemas-microsoft-com:xml-diffgram-v1">'
            f"<DocumentElement>{body}</DocumentElement></diffgr:diffgram></Value></GetDataTableWithSQLResult>")


def soap11(inner):
    return ('<?xml version="1.0" encoding="utf-8"?><soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">'
            f"<soap:Body><GetDataTableWithSQLResponse xmlns=\"http://tempuri.org/\">{inner}</GetDataTableWithSQLResponse></soap:Body></soap:Envelope>")


def soap12(op, ok, message, flag="Result"):
    return ('<?xml version="1.0" encoding="utf-8"?><soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope">'
            f'<soap:Body><{op}Response xmlns="http://tempuri.org/"><{op}Result><Message>{escape(message)}</Message>'
            f"<{flag}>{'true' if ok else 'false'}</{flag}></{op}Result></{op}Response></soap:Body></soap:Envelope>")


def like_filter(rows, sql, code_key, name_key):
    m = re.search(r"LIKE '%([^%']*)%'", sql)
    if m:
        t = m.group(1).upper()
        rows = [r for r in rows if t in str(r[code_key]).upper() or t in str(r[name_key]).upper()]
    p = re.search(rf"{code_key} LIKE '([^%']+)%'", sql)
    if p:
        rows = [r for r in rows if str(r[code_key]).startswith(p.group(1))]
    inn = re.search(rf"{code_key} IN \(([^)]*)\)", sql)
    if inn:
        codes = re.findall(r"'([^']*)'", inn.group(1))
        rows = [r for r in rows if r[code_key] in codes]
    eq = re.search(rf"{code_key} = '([^']*)'", sql)
    if eq:
        rows = [r for r in rows if r[code_key] == eq.group(1)]
    return rows


def query(sql):
    s = " ".join(sql.split())
    if "COUNT(*)" in s and "FIND_ENTITY" in s:
        return [{"N": len(ENTITIES)}]
    if "COUNT(*)" in s and "INVD_ITEM" in s:
        return [{"N": len(ITEMS)}]
    if "PSMT_OFFER_M" in s:
        doc = re.search(r"DOC_NO = '([^']*)'", s).group(1)
        o = state["offers"].get(doc)
        return [] if not o else [{"OFFER_M_ID": o["id"], "OFFER_D_ID": d, "LINE_NO": n + 1} for n, d in enumerate(o["lines"])]
    if "FIND_ENTITY" in s:
        rows = like_filter(ENTITIES, s, "ENTITY_CODE", "ENTITY_NAME")
        if "ENTITY_ID AS ID" in s:
            return [{"ID": r["ENTITY_ID"]} for r in rows]
        return rows
    if "INVD_ITEM" in s:
        rows = like_filter(ITEMS, s.replace("ITE.", ""), "ITEM_CODE", "ITEM_NAME")
        if "ITEM_ID AS ID" in s:
            return [{"ID": r["ITEM_ID"]} for r in rows]
        return rows
    raise ValueError("Sahte ERP bu sorguyu tanımıyor: " + s[:200])


def text(el, name):
    f = el.find(f".//{TEM}{name}")
    return f.text if f is not None else None


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def send(self, code, body, ctype="text/xml; charset=utf-8"):
        data = body.encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path.startswith("/_requests"):
            return self.send(200, json.dumps({"requests": state["requests"], "offers": state["offers"], "orders": state["orders"],
                                              "revisions": state["revisions"]}, ensure_ascii=False), "application/json; charset=utf-8")
        self.send(404, "not found", "text/plain")

    def do_POST(self):
        raw = self.rfile.read(int(self.headers.get("Content-Length", 0))).decode("utf-8")
        if self.path.startswith("/_reset"):
            state.update({"requests": [], "offers": {}, "orders": {}, "revisions": []})
            return self.send(200, "{}", "application/json")
        root = ET.fromstring(raw)
        body = next(iter(root.find("{http://schemas.xmlsoap.org/soap/envelope/}Body") or root.find("{http://www.w3.org/2003/05/soap-envelope}Body")))
        op = body.tag.replace(TEM, "")
        user, pwd = text(body, "UserName"), text(body, "Password")
        value = body.find(f".//{TEM}Value")
        state["requests"].append({"op": op, "user": user, "body": raw[:6000]})

        if op == "GetDataTableWithSQL":
            if (user, pwd) != (USER, PASSWORD):
                return self.send(200, soap11('<GetDataTableWithSQLResult xmlns="http://tempuri.org/"><Result>false</Result>'
                                             '<Message>Kullanıcı adı veya şifre hatalı</Message></GetDataTableWithSQLResult>'))
            try:
                return self.send(200, soap11(table(query(value.text or ""))))
            except Exception as e:  # noqa: BLE001
                return self.send(200, soap11(f'<GetDataTableWithSQLResult xmlns="http://tempuri.org/"><Result>false</Result><Message>{escape(str(e))}</Message></GetDataTableWithSQLResult>'))

        flag = "Success" if op == "SaveUyumObjectFromXML" else "Result"
        if (user, pwd) != (USER, PASSWORD):
            return self.send(200, soap12(op, False, "Kullanıcı adı veya şifre hatalı", flag))

        if op == "SaveEntity":
            code = text(value, "EntityCode")
            if code == "FAIL":
                return self.send(200, soap12(op, False, "Cari kodu geçersiz", flag))
            if not any(e["ENTITY_CODE"] == code for e in ENTITIES):
                ENTITIES.append({"ENTITY_ID": 700 + len(ENTITIES), "ENTITY_CODE": code, "ENTITY_NAME": text(value, "EntityName"), "TAX_NO": text(value, "TaxNo"), "TEL1": None, "EMAIL": None})
            return self.send(200, soap12(op, True, "Cari kaydedildi", flag))
        if op == "SaveItem":
            code = text(value, "ItemCode")
            if not any(i["ITEM_CODE"] == code for i in ITEMS):
                ITEMS.append({"ITEM_ID": 9100 + len(ITEMS), "ITEM_CODE": code, "ITEM_NAME": text(value, "ItemName"), "UNIT_CODE": text(value, "UnitCode")})
            return self.send(200, soap12(op, True, "Stok kartı kaydedildi", flag))
        if op in ("SaveOffer", "SaveOrderM"):
            doc = text(value, "DocNo")
            tag = "OfferDetailDef" if op == "SaveOffer" else "OrderDetailDef"
            details = value.findall(f".//{TEM}{tag}")
            entity = text(value, "EntityCode")
            if not any(e["ENTITY_CODE"] == entity for e in ENTITIES):
                return self.send(200, soap12(op, False, f"Cari bulunamadı: {entity}", flag))
            unknown = [text(d, "ItemCode") for d in details if not any(i["ITEM_CODE"] == text(d, "ItemCode") for i in ITEMS)]
            if unknown:
                return self.send(200, soap12(op, False, "Stok bulunamadı: " + ", ".join(unknown), flag))
            target = state["offers"] if op == "SaveOffer" else state["orders"]
            if doc in target:
                return self.send(200, soap12(op, False, f"{doc} belge numarası zaten var", flag))
            state["next_offer_m"] += 1
            lines = []
            for _ in details:
                state["next_offer_d"] += 1
                lines.append(state["next_offer_d"])
            target[doc] = {"id": state["next_offer_m"], "lines": lines, "entity": entity, "docTra": text(value, "DocTraCode"),
                           "items": [[text(d, "ItemCode"), text(d, "Qty"), text(d, "UnitPriceTra")] for d in details]}
            return self.send(200, soap12(op, True, "Belge kaydedildi", flag))
        if op == "SaveUyumObjectFromXML":
            uyum = ET.fromstring(value.text.strip())
            mid = int(uyum.find("Id").text)
            offer = next((o for o in state["offers"].values() if o["id"] == mid), None)
            if offer is None:
                return self.send(200, soap12(op, False, f"Teklif bulunamadı: {mid}", flag))
            prices = {int(d.find("Id").text): d.find("UnitPriceTra").text for d in uyum.findall("UyumDetailItem")}
            bad = [i for i in prices if i not in offer["lines"]]
            if bad:
                return self.send(200, soap12(op, False, f"Satır bulunamadı: {bad}", flag))
            state["revisions"].append({"offerMId": mid, "prices": prices})
            return self.send(200, soap12(op, True, "", flag))
        return self.send(500, soap12(op, False, "Desteklenmeyen işlem", flag))


if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8099
    print(f"fake UyumERP on http://localhost:{port}", flush=True)
    ThreadingHTTPServer(("127.0.0.1", port), Handler).serve_forever()
