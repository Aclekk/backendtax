-- Align t_invoice_download with current EF mapping (created_by / created_at).
-- Safe to re-run. Does not touch CEGID.

DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 't_invoice_download' AND column_name = 'downloaded_by'
  ) AND NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 't_invoice_download' AND column_name = 'created_by'
  ) THEN
    ALTER TABLE t_invoice_download RENAME COLUMN downloaded_by TO created_by;
  END IF;

  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 't_invoice_download' AND column_name = 'downloaded_at'
  ) AND NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 't_invoice_download' AND column_name = 'created_at'
  ) THEN
    ALTER TABLE t_invoice_download RENAME COLUMN downloaded_at TO created_at;
  END IF;
END $$;

ALTER TABLE t_invoice_download
ADD COLUMN IF NOT EXISTS created_at timestamp DEFAULT now() NOT NULL,
ADD COLUMN IF NOT EXISTS created_by varchar(100) NULL,
ADD COLUMN IF NOT EXISTS updated_at timestamp NULL,
ADD COLUMN IF NOT EXISTS updated_by varchar(100) NULL,
ADD COLUMN IF NOT EXISTS status int4 DEFAULT 1 NOT NULL;

ALTER TABLE t_faktur_pajak
ADD COLUMN IF NOT EXISTS created_at timestamp DEFAULT now() NOT NULL,
ADD COLUMN IF NOT EXISTS created_by varchar(100) NULL,
ADD COLUMN IF NOT EXISTS updated_at timestamp NULL,
ADD COLUMN IF NOT EXISTS updated_by varchar(100) NULL,
ADD COLUMN IF NOT EXISTS status int4 DEFAULT 1 NOT NULL;

ALTER TABLE t_faktur_pajak_download
ADD COLUMN IF NOT EXISTS created_at timestamp DEFAULT now() NOT NULL,
ADD COLUMN IF NOT EXISTS created_by varchar(100) NULL,
ADD COLUMN IF NOT EXISTS updated_at timestamp NULL,
ADD COLUMN IF NOT EXISTS updated_by varchar(100) NULL,
ADD COLUMN IF NOT EXISTS status int4 DEFAULT 1 NOT NULL;

-- Old NOT NULL leftover columns must not block inserts.
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 't_invoice_download' AND column_name = 'downloaded_by'
  ) THEN
    ALTER TABLE t_invoice_download ALTER COLUMN downloaded_by DROP NOT NULL;
  END IF;
END $$;
