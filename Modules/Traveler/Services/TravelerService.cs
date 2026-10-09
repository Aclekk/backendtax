using Microsoft.EntityFrameworkCore;
using TaxRefund.Api.Modules.Traveler.DTOs;
using TaxRefund.Api.Shared.Database;
using TaxRefund.Api.Shared.Database.Entities;

namespace TaxRefund.Api.Modules.Traveler.Services;

public class TravelerService(TaxRefundDbContext dbContext) : ITravelerService
{
    private readonly TaxRefundDbContext _dbContext = dbContext;

    private async Task<HashSet<int>> GetProcessedTravelerIdsAsync()
    {
        var travelerIds = await _dbContext.TTravelers
            .AsNoTracking()
            .Where(traveler =>
                _dbContext.TInvoices.Any(invoice =>
                    invoice.CreatedAt > traveler.UpdatedAt && (
                        (!string.IsNullOrWhiteSpace(traveler.PassportNumber)
                            && invoice.NomorDokumenPembeli == traveler.PassportNumber)
                        || (string.IsNullOrWhiteSpace(traveler.PassportNumber)
                            && !string.IsNullOrWhiteSpace(traveler.FullName)
                            && !string.IsNullOrWhiteSpace(traveler.Email)
                            && invoice.NamaPembeli == traveler.FullName
                            && invoice.EmailPembeli == traveler.Email))))
            .Select(traveler => traveler.Id)
            .ToListAsync();

        return travelerIds.ToHashSet();
    }

    public async Task<List<TravelerResponse>> GetTravelersAsync(string? passportNumber = null)
    {
        var processedTravelerIds = await GetProcessedTravelerIdsAsync();
        var query = _dbContext.TTravelers
            .Where(x => !processedTravelerIds.Contains(x.Id))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(passportNumber))
        {
            query = query.Where(x => x.PassportNumber.Contains(passportNumber));
        }

        var travelers = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TravelerResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                Address = x.Address,
                Nationality = x.Nationality,
                PassportNumber = x.PassportNumber,
                PassportType = x.PassportType,
                BirthDate = x.BirthDate,
                BirthPlace = x.BirthPlace,
                PassportExpiryDate = x.PassportExpiryDate,
                SourceChannel = x.SourceChannel,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        // PostgreSQL stores these legacy timestamps without timezone metadata,
        // but CreatedAt has always been persisted as UTC.
        foreach (var traveler in travelers)
        {
            traveler.CreatedAt = DateTime.SpecifyKind(traveler.CreatedAt, DateTimeKind.Utc);
            traveler.UpdatedAt = DateTime.SpecifyKind(traveler.UpdatedAt, DateTimeKind.Utc);
        }

        return travelers;
    }

    public async Task<List<TravelerHistoryListResponse>> GetHistoryListAsync()
    {
        var processedTravelerIds = await GetProcessedTravelerIdsAsync();

        var travelers = await _dbContext.TTravelers
            .AsNoTracking()
            .Where(x => processedTravelerIds.Contains(x.Id))
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new TravelerHistoryListResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                Address = x.Address,
                Nationality = x.Nationality,
                PassportNumber = x.PassportNumber,
                SourceChannel = x.SourceChannel,
                CreatedAt = x.CreatedAt,
                LastActivityAt = x.UpdatedAt
            })
            .ToListAsync();

        foreach (var traveler in travelers)
        {
            var latestInvoice = await _dbContext.TInvoices
                .AsNoTracking()
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(traveler.PassportNumber)
                        && x.NomorDokumenPembeli == traveler.PassportNumber
                        && x.CreatedAt > traveler.CreatedAt)
                    || (string.IsNullOrWhiteSpace(traveler.PassportNumber)
                        && !string.IsNullOrWhiteSpace(traveler.FullName)
                        && !string.IsNullOrWhiteSpace(traveler.Email)
                        && x.NamaPembeli == traveler.FullName
                        && x.EmailPembeli == traveler.Email
                        && x.CreatedAt > traveler.CreatedAt))
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestInvoice != default)
            {
                traveler.LastActivityAt = latestInvoice > (traveler.LastActivityAt ?? DateTime.MinValue)
                    ? latestInvoice
                    : traveler.LastActivityAt;
            }
        }

        return travelers
            .OrderByDescending(x => x.LastActivityAt ?? x.CreatedAt)
            .ToList();
    }

    public async Task<TravelerDetailResponse?> GetTravelerAsync(int id)
    {
        var traveler = await _dbContext.TTravelers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (traveler is null)
        {
            return null;
        }

        return new TravelerDetailResponse
        {
            Id = traveler.Id,
            FullName = traveler.FullName,
            Email = traveler.Email,
            Address = traveler.Address,
            Nationality = traveler.Nationality,
            PassportNumber = traveler.PassportNumber,
            PassportType = traveler.PassportType,
            BirthDate = traveler.BirthDate,
            BirthPlace = traveler.BirthPlace,
            PassportExpiryDate = traveler.PassportExpiryDate,
            SourceChannel = traveler.SourceChannel,
            CreatedAt = traveler.CreatedAt,
            UpdatedAt = traveler.UpdatedAt
        };
    }

    public async Task<TravelerResponse> CreateTravelerAsync(CreateTravelerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Nama lengkap wajib diisi.");

        if (string.IsNullOrWhiteSpace(request.Nationality))
            throw new ArgumentException("Kewarganegaraan wajib diisi.");

        if (string.IsNullOrWhiteSpace(request.PassportNumber))
            throw new ArgumentException("Nomor paspor wajib diisi.");

        var normalizedPassportNumber = request.PassportNumber.Trim();
        var existingTraveler = await _dbContext.TTravelers
            .FirstOrDefaultAsync(x => x.PassportNumber == normalizedPassportNumber);

        if (existingTraveler is not null)
        {
            var updatedAtNow = DateTime.UtcNow;
            existingTraveler.FullName = request.FullName.Trim();
            existingTraveler.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            existingTraveler.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
            existingTraveler.Nationality = request.Nationality.Trim();
            existingTraveler.PassportType = string.IsNullOrWhiteSpace(request.PassportType) ? null : request.PassportType.Trim();
            existingTraveler.BirthDate = request.BirthDate;
            existingTraveler.BirthPlace = string.IsNullOrWhiteSpace(request.BirthPlace) ? null : request.BirthPlace.Trim();
            existingTraveler.PassportExpiryDate = request.PassportExpiryDate;
            existingTraveler.SourceChannel = string.IsNullOrWhiteSpace(request.SourceChannel) ? "tourist_self_service" : request.SourceChannel.Trim();
            existingTraveler.UpdatedAt = updatedAtNow;

            await _dbContext.SaveChangesAsync();

            return new TravelerResponse
            {
                Id = existingTraveler.Id,
                FullName = existingTraveler.FullName,
                Email = existingTraveler.Email,
                Address = existingTraveler.Address,
                Nationality = existingTraveler.Nationality,
                PassportNumber = existingTraveler.PassportNumber,
                PassportType = existingTraveler.PassportType,
                BirthDate = existingTraveler.BirthDate,
                BirthPlace = existingTraveler.BirthPlace,
                PassportExpiryDate = existingTraveler.PassportExpiryDate,
                SourceChannel = existingTraveler.SourceChannel,
                CreatedAt = existingTraveler.CreatedAt,
                UpdatedAt = existingTraveler.UpdatedAt
            };
        }

        var now = DateTime.UtcNow;
        var traveler = new TTraveler
        {
            FullName = request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Nationality = request.Nationality.Trim(),
            PassportNumber = normalizedPassportNumber,
            PassportType = string.IsNullOrWhiteSpace(request.PassportType) ? null : request.PassportType.Trim(),
            BirthDate = request.BirthDate,
            BirthPlace = string.IsNullOrWhiteSpace(request.BirthPlace) ? null : request.BirthPlace.Trim(),
            PassportExpiryDate = request.PassportExpiryDate,
            SourceChannel = string.IsNullOrWhiteSpace(request.SourceChannel) ? "tourist_self_service" : request.SourceChannel.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.TTravelers.Add(traveler);
        await _dbContext.SaveChangesAsync();

        return new TravelerResponse
        {
            Id = traveler.Id,
            FullName = traveler.FullName,
            Email = traveler.Email,
            Address = traveler.Address,
            Nationality = traveler.Nationality,
            PassportNumber = traveler.PassportNumber,
            PassportType = traveler.PassportType,
            BirthDate = traveler.BirthDate,
            BirthPlace = traveler.BirthPlace,
            PassportExpiryDate = traveler.PassportExpiryDate,
            SourceChannel = traveler.SourceChannel,
            CreatedAt = traveler.CreatedAt,
            UpdatedAt = traveler.UpdatedAt
        };
    }

}
