using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Repositories;

public class AppointmentRepository
{
    private readonly ApplicationDbContext _context;

    public AppointmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        return await _context.Appointments
            .Include(a => a.Transaction)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<Appointment?> GetByIdAsync(int id)
    {
        return await _context.Appointments.FindAsync(id);
    }

    public async Task<Appointment> AddAsync(Appointment appointment)
    {
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();
        return appointment;
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment != null)
        {
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Appointment>> GetAppointmentsByDateAndMechanicAsync(DateTime date, string? mechanic)
    {
        var query = _context.Appointments
            .Where(a => a.AppointmentDate.Date == date.Date
                && (a.Status == "Confirmed" || a.Status == "Pending"));

        if (!string.IsNullOrEmpty(mechanic) && mechanic != "Any Available")
        {
            query = query.Where(a => a.MechanicName == mechanic);
        }

        return await query.ToListAsync();
    }

    public async Task<List<Appointment>> GetByCustomerNameAsync(string customerName)
    {
        return await _context.Appointments
            .Include(a => a.Transaction)
            .Where(a => a.CustomerName == customerName)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetByCustomerIdentityAsync(
        string userId,
        string email,
        string fullName)
    {
        return await _context.Appointments
            .Include(a => a.Transaction)
            .Where(a => a.CustomerUserId == userId
                || (a.CustomerUserId == null && a.CustomerEmail == email)
                || (a.CustomerUserId == null && a.CustomerEmail == null
                    && (a.CustomerName == email || a.CustomerName == fullName)))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}
