-- Tohum anlık görüntüsündeki tüm zaman damgalarını bugüne kaydırır: anlık görüntü alındığı andaki göreli durum
-- (ör. "2 gün sonra başlayacak ihale", "1 saat önce başlamış canlı ihale") geri yükleme günü ne olursa olsun korunur.
-- Referans an: en son oluşturulan ihale (tohumlamanın sonu). Denetim kaydı referans olamaz: API açılınca zamanlayıcı
-- hemen yeni kayıt yazar. restore.sh migration'lardan sonra, API'yi başlatmadan çalıştırır.
DO $$
DECLARE
  r record;
  delta interval;
BEGIN
  SELECT now() - max("CreatedAt") INTO delta FROM "Auctions";
  IF delta IS NULL OR delta < interval '1 hour' THEN
    RAISE NOTICE 'shift: gerek yok';
    RETURN;
  END IF;

  FOR r IN
    SELECT c.table_name, c.column_name
    FROM information_schema.columns c
    JOIN information_schema.tables t ON t.table_schema = c.table_schema AND t.table_name = c.table_name
    WHERE c.table_schema = 'public' AND c.data_type = 'timestamp with time zone' AND t.table_type = 'BASE TABLE' -- görünümler (analitik) hariç
  LOOP
    EXECUTE format('UPDATE %I SET %I = %I + $1 WHERE %I IS NOT NULL', r.table_name, r.column_name, r.column_name, r.column_name) USING delta;
  END LOOP;

  RAISE NOTICE 'shift: %', delta;
END $$;
