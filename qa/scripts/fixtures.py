# -*- coding: utf-8 -*-
"""Tohum verisinden test fixture'ı üretir: kayıt kimlikleri (başlıklara göre) ve hesaplar → qa/data/fixtures.json.
Kimlikler her tohumlamada değişir; testler adları değil bu dosyayı kullanır."""
import json
import os
import subprocess

QA = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def q(sql):
    env = dict(os.environ, MSYS_NO_PATHCONV="1")
    out = subprocess.run(["docker", "exec", "procurelive-postgres", "psql", "-tA", "-F", "|", "-U", "procurelive_user", "-d", "procurelive", "-c", sql],
                         capture_output=True, text=True, encoding="utf-8", env=env, check=True).stdout
    return [line.split("|") for line in out.strip().splitlines() if line]


AUCTIONS = {
    "A1": "2027 Yılı Kırtasiye Çerçeve Alımı", "A2": "Aliağa Santrali Kablo ve Pano Alımı", "A3": "Konya Tesisi Ambalaj Malzemesi Alımı",
    "A4": "Genel Müdürlük BT Donanım Yenileme", "A5": "Konya Tesisi Un ve Şeker Alımı", "A6": "Gebze Aktarma Merkezi Hurda Palet ve Metal Satışı",
    "A7": "Kartal Şantiyesi Hazır Beton Alımı", "A8": "Manisa Depo Raf Sistemi Alımı", "A9": "Genel Müdürlük Ofis Mobilyası Alımı",
    "A10": "Bursa RES Kablo Alımı", "A11": "Mersin Liman Operasyonu Forklift Kiralama Hizmeti", "A12": "Eskişehir Köprü Projesi İnşaat Demiri Alımı",
    "A13": "Ankara Bölge Kırtasiye ve Sarf Malzemesi", "A14": "Aliağa Santrali Yangın Söndürme Ekipmanı Alımı", "A15": "Bursa RES Trafo Bakım Malzemesi Alımı",
}
RFI = {"RFI1": "2027 Tedarikçi Ön Yeterlilik Turu", "RFI2": "BT Tedarikçileri Bilgi Güvenliği Değerlendirmesi", "RFI3": "Konya Tesisi Gıda Güvenliği Değerlendirmesi",
       "RFI4": "Kartal Şantiyesi İSG ve Çevre Uygunluk", "RFI5": "Lojistik Kapasite Değerlendirmesi 2026", "RFI6": "Enerji Ekipmanı Tedarikçi Ön Yeterlilik"}
RFQ = {"Q1": "Ankara Bölge Toner ve Sarf Malzemesi", "Q2": "Aliağa Santrali Yedek Parça Fiyat Talebi", "Q3": "Konya Tesisi Koli ve Streç Film Fiyat Talebi",
       "Q4": "Gebze Forklift Bakım Hizmeti Fiyat Talebi", "Q5": "Genel Müdürlük Monitör Alımı", "Q6": "Kartal Şantiyesi Demir Fiyat Talebi"}

by_title = {t: i for i, t in q('select "Id", "Title" from "Auctions"')}
rfi_by = {t: i for i, t in q('select "Id", "Title" from "RfiProcesses"')}
rfq_by = {t: i for i, t in q('select "Id", "Title" from "RfqProcesses"')}
firms = {c: i for i, c in q('select "Id", "Code" from "Companies" where "CompanyType"=0')}
companies = {n: {"id": i, "code": c} for i, n, c in q('select "Id", "Name", coalesce("Code", \'\') from "Companies" where "CompanyType"<>0')}
workspaces = {n: i for i, n in q('select "Id", "Name" from "Workspaces"')}
users = [dict(email=e, name=f"{f} {l}", companyId=c or None, active=a == "t", locked=k != "")
         for e, f, l, c, a, k in q('select "Email", "FirstName", "LastName", coalesce("CompanyId"::text, \'\'), "IsActive", coalesce("LockedOutAt"::text, \'\') from "AspNetUsers" order by "Email"')]
roles = {e: r for e, r in q('select u."Email", string_agg(r."Name", \',\') from "AspNetUsers" u join "AspNetUserRoles" ur on ur."UserId"=u."Id" join "AspNetRoles" r on r."Id"=ur."RoleId" group by u."Email"')}
for u in users:
    u["roles"] = (roles.get(u["email"]) or "").split(",")
orders = [dict(id=i, number=n, status=int(s), supplierCompanyId=c) for i, n, s, c in q('select "Id", "OrderNumber", "Status", "SupplierCompanyId" from "PurchaseOrders" order by "OrderNumber"')]
links = [dict(token=t, company=n, auction=a, email=e or None, used=u == "t")
         for t, n, a, e, u in q('select it."Token", c."Name", a."Title", it."Email", (it."UsedAt" is not null) from "InvitationTokens" it join "Companies" c on c."Id"=it."CompanyId" join "Auctions" a on a."Id"=it."AuctionId"')]

fx = {
    "password": "Test1234!",
    "admin": {"email": "admin@procurelive.local", "password": "ChangeMe123!"},
    "auctions": {k: by_title[v] for k, v in AUCTIONS.items()},
    "rfi": {k: rfi_by[v] for k, v in RFI.items()},
    "rfq": {k: rfq_by[v] for k, v in RFQ.items()},
    "firms": firms, "companies": companies, "workspaces": workspaces, "users": users, "orders": orders, "links": links,
}
os.makedirs(os.path.join(QA, "data"), exist_ok=True)
json.dump(fx, open(os.path.join(QA, "data", "fixtures.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("fixtures:", len(fx["auctions"]), "ihale,", len(companies), "cari,", len(users), "kullanıcı,", len(orders), "sipariş,", len(links), "link")
