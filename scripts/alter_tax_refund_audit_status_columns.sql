-- Add audit columns and status flag to tax_refund tables.
-- Note: t_email_log already has a status enum column, so only audit columns are added there.

ALTER TABLE t_faktur_pajak
ADD COLUMN IF NOT EXISTS status int4 DEFAULT 1 NOT NULL;

ALTER TABLE t_faktur_pajak_detail
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

ALTER TABLE t_email_log
ADD COLUMN IF NOT EXISTS created_by varchar(100) NULL,
ADD COLUMN IF NOT EXISTS updated_at timestamp NULL,
ADD COLUMN IF NOT EXISTS updated_by varchar(100) NULL;

ALTER TABLE m_negara
ADD COLUMN IF NOT EXISTS created_by varchar(100) NULL,
ADD COLUMN IF NOT EXISTS updated_by varchar(100) NULL,
ADD COLUMN IF NOT EXISTS status int4 DEFAULT 1 NOT NULL;

ALTER TABLE t_invoice_download
ADD COLUMN IF NOT EXISTS created_at timestamp DEFAULT now() NOT NULL,
ADD COLUMN IF NOT EXISTS created_by varchar(100) NULL,
ADD COLUMN IF NOT EXISTS updated_at timestamp NULL,
ADD COLUMN IF NOT EXISTS updated_by varchar(100) NULL,
ADD COLUMN IF NOT EXISTS status int4 DEFAULT 1 NOT NULL;
