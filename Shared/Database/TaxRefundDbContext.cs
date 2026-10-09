using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TaxRefund.Api.Shared.Database.Entities;
using TaxRefund.Api.Shared.Database.Seed;

namespace TaxRefund.Api.Shared.Database;

public partial class TaxRefundDbContext : DbContext
{
    public TaxRefundDbContext(DbContextOptions<TaxRefundDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TEmailLog> TEmailLogs { get; set; }

    public virtual DbSet<TFakturPajak> TFakturPajaks { get; set; }

    public virtual DbSet<TFakturPajakDetail> TFakturPajakDetails { get; set; }

    public virtual DbSet<TInvoice> TInvoices { get; set; }
    public virtual DbSet<TNegara> TNegaras { get; set; }
    public virtual DbSet<TTraveler> TTravelers { get; set; }

    public virtual DbSet<TInvoiceDownload> TInvoiceDownloads { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
         base.OnModelCreating(modelBuilder);
          modelBuilder.Entity<TNegara>()
           .HasData(NegaraSeedData.All);
        modelBuilder.Entity<TNegara>(entity =>
{
    entity.HasKey(e => e.Id).HasName("m_negara_pkey");

    entity.ToTable("m_negara");

    entity.HasIndex(e => e.Kode, "idx_m_negara_kode")
        .IsUnique();

    entity.Property(e => e.Id)
        .HasColumnName("id");

    entity.Property(e => e.Kode)
        .HasMaxLength(10)
        .HasColumnName("kode");

    entity.Property(e => e.Nama)
        .HasMaxLength(100)
        .HasColumnName("nama");

    entity.Property(e => e.IsActive)
        .HasDefaultValue(true)
        .HasColumnName("is_active");

    entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("now()")
        .HasColumnType("timestamp without time zone")
        .HasColumnName("created_at");

    entity.Property(e => e.UpdatedAt)
        .HasDefaultValueSql("now()")
        .HasColumnType("timestamp without time zone")
        .HasColumnName("updated_at");
});
        modelBuilder
            .HasPostgresEnum<EmailLogStatus>("email_log_status")
            .HasPostgresEnum<InvoiceStatus>("invoice_status");

        modelBuilder.Entity<TEmailLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("t_email_log_pkey");

            entity.ToTable("t_email_log");

            entity.HasIndex(e => e.FakturPajakId, "idx_email_log_faktur_pajak_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.FakturPajakId).HasColumnName("faktur_pajak_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedBy)          // was SentBy
    .HasMaxLength(100)
    .HasColumnName("created_by");

            entity.HasOne(d => d.FakturPajak).WithMany(p => p.TEmailLogs)
                .HasForeignKey(d => d.FakturPajakId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("t_email_log_faktur_pajak_id_fkey");
        });

        modelBuilder.Entity<TFakturPajak>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("t_faktur_pajak_pkey");

            entity.ToTable("t_faktur_pajak");

            entity.HasIndex(e => e.InvoiceId, "idx_faktur_pajak_invoice_id");

            entity.HasIndex(e => e.InvoiceId, "t_faktur_pajak_invoice_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EmailSentAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("email_sent_at");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .HasColumnName("file_path");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.NomorFakturPajak)
                .HasMaxLength(50)
                .HasColumnName("nomor_faktur_pajak");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.UploadedBy)
                .HasMaxLength(100)
                .HasColumnName("uploaded_by");

            entity.HasOne(d => d.Invoice).WithOne(p => p.TFakturPajak)
                .HasForeignKey<TFakturPajak>(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("t_faktur_pajak_invoice_id_fkey");
                entity.Property(e => e.CreatedAt)
    .HasDefaultValueSql("now()")
    .HasColumnType("timestamp without time zone")
    .HasColumnName("created_at");
entity.Property(e => e.CreatedBy)
    .HasMaxLength(100)
    .HasColumnName("created_by");
entity.Property(e => e.UpdatedAt)
    .HasColumnType("timestamp without time zone")
    .HasColumnName("updated_at");
entity.Property(e => e.UpdatedBy)
    .HasMaxLength(100)
    .HasColumnName("updated_by");
        });

        modelBuilder.Entity<TFakturPajakDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("t_faktur_pajak_detail_pkey");

            entity.ToTable("t_faktur_pajak_detail");

            entity.HasIndex(e => e.FakturPajakId, "idx_faktur_pajak_detail_faktur_pajak_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.BarangJasa)
                .HasMaxLength(5)
                .HasColumnName("barang_jasa");
            entity.Property(e => e.Baris).HasColumnName("baris");
            entity.Property(e => e.Dpp)
                .HasPrecision(15, 2)
                .HasColumnName("dpp");
            entity.Property(e => e.DppNilaiLain)
                .HasPrecision(15, 2)
                .HasColumnName("dpp_nilai_lain");
            entity.Property(e => e.FakturPajakId).HasColumnName("faktur_pajak_id");
            entity.Property(e => e.HargaSatuan)
                .HasPrecision(15, 2)
                .HasColumnName("harga_satuan");
            entity.Property(e => e.JumlahBarangJasa).HasColumnName("jumlah_barang_jasa");
            entity.Property(e => e.KodeBarangJasa)
                .HasMaxLength(20)
                .HasColumnName("kode_barang_jasa");
            entity.Property(e => e.NamaBarangJasa)
                .HasMaxLength(255)
                .HasColumnName("nama_barang_jasa");
            entity.Property(e => e.NamaSatuanUkur)
                .HasMaxLength(50)
                .HasColumnName("nama_satuan_ukur");
            entity.Property(e => e.Ppn)
                .HasPrecision(15, 2)
                .HasColumnName("ppn");
            entity.Property(e => e.Ppnbm)
                .HasPrecision(15, 2)
                .HasDefaultValue(0m)
                .HasColumnName("ppnbm");
            entity.Property(e => e.TarifPpn)
                .HasPrecision(5, 2)
                .HasColumnName("tarif_ppn");
            entity.Property(e => e.TarifPpnbm)
                .HasPrecision(5, 2)
                .HasDefaultValue(0m)
                .HasColumnName("tarif_ppnbm");
            entity.Property(e => e.TotalDiskon)
                .HasPrecision(15, 2)
                .HasDefaultValue(0m)
                .HasColumnName("total_diskon");

            entity.HasOne(d => d.FakturPajak).WithMany(p => p.TFakturPajakDetails)
                .HasForeignKey(d => d.FakturPajakId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("t_faktur_pajak_detail_faktur_pajak_id_fkey");
        });

       modelBuilder.Entity<TInvoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("t_invoice_pkey");

            entity.ToTable("t_invoice");

            entity.Property(e => e.CoreTaxFailureEmailSentAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("coretax_failure_email_sent_at");

            entity.HasIndex(e => e.NoInvoice, "idx_invoice_no_invoice");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AlamatPembeli).HasColumnName("alamat_pembeli");
            entity.Property(e => e.Baris).HasColumnName("baris");
            entity.Property(e => e.CapFasilitas)
                .HasMaxLength(100)
                .HasColumnName("cap_fasilitas");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.DokumenPendukung)
                .HasMaxLength(255)
                .HasColumnName("dokumen_pendukung");
            entity.Property(e => e.EmailPembeli)
                .HasMaxLength(255)
                .HasColumnName("email_pembeli");
            entity.Property(e => e.IdTkuPembeli)
                .HasMaxLength(30)
                .HasColumnName("id_tku_pembeli");
            entity.Property(e => e.IdTkuPenjual)
                .HasMaxLength(30)
                .HasColumnName("id_tku_penjual");
            entity.Property(e => e.JenisFaktur)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Normal'::character varying")
                .HasColumnName("jenis_faktur");
            entity.Property(e => e.JenisIdPembeli)
                .HasMaxLength(20)
                .HasColumnName("jenis_id_pembeli");
            entity.Property(e => e.KeteranganTambahan).HasColumnName("keterangan_tambahan");
            entity.Property(e => e.KodeTransaksi)
                .HasMaxLength(10)
                .HasDefaultValueSql("'06'::character varying")
                .HasColumnName("kode_transaksi");
            entity.Property(e => e.NamaPembeli)
                .HasMaxLength(255)
                .HasColumnName("nama_pembeli");
            entity.Property(e => e.NegaraPembeli)
                .HasMaxLength(100)
                .HasColumnName("negara_pembeli");
            entity.Property(e => e.NoInvoice)
                .HasMaxLength(50)
                .HasColumnName("no_invoice");
            entity.Property(e => e.NomorDokumenPembeli)
                .HasMaxLength(50)
                .HasColumnName("nomor_dokumen_pembeli");
            entity.Property(e => e.NpwpNikPembeli)
                .HasMaxLength(30)
                .HasColumnName("npwp_nik_pembeli");
            entity.Property(e => e.PeriodDokPendukung)
                .HasMaxLength(50)
                .HasColumnName("period_dok_pendukung");
            entity.Property(e => e.Referensi)
                .HasMaxLength(150)
                .HasColumnName("referensi");
            entity.Property(e => e.TanggalFaktur).HasColumnName("tanggal_faktur");
           entity.Property(e => e.Status).HasColumnName("status");
entity.Property(e => e.StoreCode)
    .HasMaxLength(50)
    .HasDefaultValueSql("''::character varying")
    .HasColumnName("store_code");
entity.Property(e => e.UpdatedAt)
    .HasDefaultValueSql("now()")
    .HasColumnType("timestamp without time zone")
    .HasColumnName("updated_at");

            // Mapping for nullable CustomerId FK -> t_customer.id
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");

            entity.HasOne(d => d.Customer).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("t_invoice_customer_id_fkey");
        });

        modelBuilder.Entity<TInvoiceDownload>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("t_invoice_download_pkey");

            entity.ToTable("t_invoice_download");

            entity.HasIndex(e => e.InvoiceId, "idx_invoice_download_invoice_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Invoice).WithMany(p => p.TInvoiceDownloads)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("t_invoice_download_invoice_id_fkey");
        });

        modelBuilder.Entity<TTraveler>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("t_customer_pkey");
            entity.ToTable("t_customer");

            entity.HasIndex(e => e.PassportNumber, "idx_customer_passport_number");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.Nationality)
                .HasMaxLength(100)
                .HasColumnName("nationality");
            entity.Property(e => e.PassportNumber)
                .HasMaxLength(100)
                .HasColumnName("passport_number");
            entity.Property(e => e.PassportType)
                .HasMaxLength(50)
                .HasColumnName("passport_type");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.BirthPlace)
                .HasMaxLength(255)
                .HasColumnName("birth_place");
            entity.Property(e => e.PassportExpiryDate).HasColumnName("passport_expiry_date");
            entity.Property(e => e.SourceChannel)
                .HasMaxLength(50)
                .HasDefaultValue("tourist_self_service")
                .HasColumnName("source_channel");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
