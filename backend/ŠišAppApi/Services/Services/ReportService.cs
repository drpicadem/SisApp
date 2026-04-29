using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<StatsDto> GetStatsAsync()
        {
            var totalUsers = await _context.Users.CountAsync(u => u.Role == AppRoles.User && u.IsActive);
            var totalBarbers = await _context.Barbers.CountAsync();
            var totalSalons = await _context.Salons.CountAsync();

            var currentYear = DateTime.UtcNow.Year;
            var monthlyRegistrations = await _context.Users
                .Where(u => u.Role == AppRoles.User && u.CreatedAt.Year == currentYear)
                .GroupBy(u => u.CreatedAt.Month)
                .Select(g => new MonthlyRegistrationDto { Month = g.Key, Count = g.Count() })
                .OrderBy(x => x.Month)
                .ToListAsync();

            return new StatsDto
            {
                TotalUsers = totalUsers,
                TotalBarbers = totalBarbers,
                TotalSalons = totalSalons,
                MonthlyRegistrations = monthlyRegistrations
            };
        }

        public async Task<byte[]> GenerateStatsPdfAsync()
        {
            var stats = await GetStatsAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));
                    page.Header().Element(ComposeHeader);
                    page.Content().Element(x => ComposeContent(x, stats));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Stranica ");
                        x.CurrentPageNumber();
                        x.Span(" od ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateAppointmentsPdfAsync()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Salon)
                .Include(a => a.Service)
                .Include(a => a.User)
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.AppointmentDateTime)
                .Take(200)
                .ToListAsync();

            var byStatus = appointments
                .GroupBy(a => a.Status ?? "Unknown")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Text($"Izvještaj: Rezervacije ({DateTime.UtcNow:dd.MM.yyyy})")
                        .FontSize(18).SemiBold().FontColor(Colors.Blue.Darken2);

                    page.Content().Column(column =>
                    {
                        column.Spacing(12);
                        column.Item().Text("Pregled po statusu").FontSize(14).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(90);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Padding(4).BorderBottom(1).Text("Status").SemiBold();
                                header.Cell().Padding(4).BorderBottom(1).Text("Broj").SemiBold();
                            });
                            foreach (var row in byStatus)
                            {
                                table.Cell().Padding(4).Text(row.Status);
                                table.Cell().Padding(4).Text(row.Count.ToString());
                            }
                        });

                        column.Item().Text("Zadnje rezervacije").FontSize(14).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.4f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.0f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Padding(4).BorderBottom(1).Text("Datum").SemiBold();
                                header.Cell().Padding(4).BorderBottom(1).Text("Salon").SemiBold();
                                header.Cell().Padding(4).BorderBottom(1).Text("Usluga").SemiBold();
                                header.Cell().Padding(4).BorderBottom(1).Text("Status").SemiBold();
                            });
                            foreach (var a in appointments.Take(40))
                            {
                                table.Cell().Padding(4).Text(a.AppointmentDateTime.ToString("dd.MM.yyyy HH:mm"));
                                table.Cell().Padding(4).Text(a.Salon?.Name ?? "-");
                                table.Cell().Padding(4).Text(a.Service?.Name ?? "-");
                                table.Cell().Padding(4).Text(a.Status ?? "-");
                            }
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateRevenuePdfAsync()
        {
            var paidQuery = _context.Payments
                .Where(p => p.Status == PaymentStatuses.Completed && !p.IsDeleted);

            var totalRevenue = await paidQuery.SumAsync(p => p.Amount);

            var bySalon = await paidQuery
                .Where(p => p.Appointment != null && p.Appointment.Salon != null)
                .GroupBy(p => p.Appointment!.Salon!.Name)
                .Select(g => new { Salon = g.Key, Revenue = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            var byMonthRaw = await paidQuery
                .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(x => x.Amount) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var byMonth = byMonthRaw
                .Select(x => new { Period = $"{x.Month:D2}/{x.Year}", x.Revenue })
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Text($"Izvještaj: Prihodi ({DateTime.UtcNow:dd.MM.yyyy})")
                        .FontSize(18).SemiBold().FontColor(Colors.Blue.Darken2);

                    page.Content().Column(column =>
                    {
                        column.Spacing(12);
                        column.Item().Text($"Ukupan prihod: {totalRevenue:0.00}").FontSize(14).SemiBold();

                        column.Item().Text("Prihod po salonu").FontSize(14).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.6f);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(100);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Padding(4).BorderBottom(1).Text("Salon").SemiBold();
                                header.Cell().Padding(4).BorderBottom(1).Text("Uplata").SemiBold();
                                header.Cell().Padding(4).BorderBottom(1).Text("Prihod").SemiBold();
                            });
                            foreach (var row in bySalon.Take(20))
                            {
                                table.Cell().Padding(4).Text(row.Salon);
                                table.Cell().Padding(4).Text(row.Count.ToString());
                                table.Cell().Padding(4).Text($"{row.Revenue:0.00}");
                            }
                        });

                        column.Item().Text("Prihod po mjesecima").FontSize(14).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            table.Header(header =>
                            {
                                header.Cell().Padding(4).BorderBottom(1).Text("Period").SemiBold();
                                header.Cell().Padding(4).BorderBottom(1).Text("Prihod").SemiBold();
                            });
                            foreach (var row in byMonth)
                            {
                                table.Cell().Padding(4).Text(row.Period);
                                table.Cell().Padding(4).Text($"{row.Revenue:0.00}");
                            }
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Izvještaj: Statistika ŠišApp").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text(text =>
                    {
                        text.Span("Datum izrade: ").SemiBold();
                        text.Span($"{DateTime.UtcNow:dd.MM.yyyy}");
                    });
                });
            });
        }

        private static void ComposeContent(IContainer container, StatsDto stats)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(20);
                column.Item().Text("Opšti pregled").FontSize(16).SemiBold();

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        header.Cell().BorderBottom(1).Padding(5).Text("Korisnici").SemiBold();
                        header.Cell().BorderBottom(1).Padding(5).Text("Frizeri").SemiBold();
                        header.Cell().BorderBottom(1).Padding(5).Text("Saloni").SemiBold();
                    });
                    table.Cell().Padding(5).Text(stats.TotalUsers.ToString());
                    table.Cell().Padding(5).Text(stats.TotalBarbers.ToString());
                    table.Cell().Padding(5).Text(stats.TotalSalons.ToString());
                });

                column.Item().Text("Mjesečne registracije korisnika").FontSize(16).SemiBold();

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        header.Cell().BorderBottom(1).Padding(5).Text("Mjesec").SemiBold();
                        header.Cell().BorderBottom(1).Padding(5).Text("Broj novih korisnika (za trenutnu godinu)").SemiBold();
                    });
                    foreach (var item in stats.MonthlyRegistrations)
                    {
                        table.Cell().Padding(5).Text(item.Month.ToString());
                        table.Cell().Padding(5).Text(item.Count.ToString());
                    }
                });
            });
        }
    }
}
