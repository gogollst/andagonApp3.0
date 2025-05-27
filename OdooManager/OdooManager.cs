using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OdooRpc.CoreCLR.Client.V8;
using OdooRpc.CoreCLR.Client.V8.Models;
using OdooRpc.CoreCLR.Client.V8.Models.Parameters;

namespace OdooManager
{
    public class OdooManager
    {
        private OdooRpcClient odooClient;

        public OdooManager()
        {
        }

//Authentifizierung
        public async Task Authenticate()
        {
            var connection = new OdooConnectionInfo
            {
                Host = "andagon-holding.cloud",
                IsSSL = true,
                Port = 443,
                Database = "live",
                Username = "s.gogoll@andagon.com",
                Password = "465eb6b1b80967654bfb77f05357ccfc8a3db974"
            };
            odooClient = new OdooRpcClient(connection);
            await odooClient.Authenticate();
        }

// 1. Arbeitszeiterfassung
        public async Task<double> GetTotalWorkHours(long employeeId, DateTime startDate, DateTime endDate)
        {
            var searchParams = new OdooSearchParameters("hr.attendance", new OdooDomainFilter()
                .Filter("employee_id", "=", employeeId)
                .Filter("check_in", ">=", startDate.ToString("yyyy-MM-dd"))
                .Filter("check_in", "<=", endDate.ToString("yyyy-MM-dd"))
            );
            var attendances = await odooClient.Get<HrAttendance[]>(searchParams);
            double totalHours = attendances.Sum(a => (DateTime.Parse(a.check_out) - DateTime.Parse(a.check_in)).TotalHours);
            return totalHours;
        }

// 2. Projektzeiterfassung
        public async Task<List<TimesheetResult>> GetTimesheetsByMonth(DateTime month, long employeeId)
        {
            var firstDay = new DateTime(month.Year, month.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            string dateFrom = firstDay.ToString("yyyy-MM-dd");
            string dateTo = lastDay.ToString("yyyy-MM-dd");

            var domainFilter = new OdooDomainFilter()
                .Filter("employee_id", "=", employeeId)
                .Filter("date", ">=", dateFrom)
                .Filter("date", "<=", dateTo);

            var fieldParams = new OdooFieldParameters(new List<string> { "id", "date", "name", "unit_amount" });
            var searchParams = new OdooSearchParameters("account.analytic.line", domainFilter);

            var result = await odooClient.Get<Object[]>(searchParams, fieldParams);
            if (result == null || result.Length == 0) return new List<TimesheetResult>();

            var json = System.Text.Json.JsonSerializer.Serialize(result);
            var timesheets = JsonConvert.DeserializeObject<List<TimesheetResult>>(json);
            if (timesheets != null)
            {
                foreach (var t in timesheets)
                {
                    if (string.IsNullOrEmpty(t.description)) t.description = "";
                }
            }
            return timesheets ?? new List<TimesheetResult>();
        }

// 3. Urlaubsantrag
        public async Task RequestVacationLeave(long employeeId, DateTime startDate, int numberOfDays, string description = "Urlaubsantrag")
        {
            string dateFrom = startDate.ToString("yyyy-MM-dd");
            string dateTo = startDate.AddDays(numberOfDays - 1).ToString("yyyy-MM-dd");

            long leaveTypeId = await GetLeaveTypeIdByName("Urlaub");
            if (leaveTypeId == 0)
            {
                throw new Exception("Leave type 'Urlaub' not found.");
            }

            var leaveData = new Dictionary<string, object>
            {
                { "employee_id", employeeId },
                { "holiday_status_id", leaveTypeId },
                { "date_from", $"{dateFrom} 08:00:00" },
                { "date_to", $"{dateTo} 17:00:00" },
                { "number_of_days", numberOfDays },
                { "name", description },
                { "state", "confirm" }
            };

            var newLeaveId = await odooClient.Create("hr.leave", leaveData);
            Console.WriteLine($"Urlaubsantrag mit ID {newLeaveId} erstellt.");
        }

        private async Task<long> GetLeaveTypeIdByName(string leaveTypeName)
        {
            var searchParams = new OdooSearchParameters("hr.leave.type", new OdooDomainFilter()
                .Filter("name", "=", leaveTypeName));
            var leaveTypes = await odooClient.Get<SimpleResult[]>(searchParams);
            return leaveTypes.FirstOrDefault()?.id ?? 0;
        }

// 4. Spesenabrechnung
        public async Task CreateExpense(long employeeId, DateTime expenseDate, string description, double amount, long productId, byte[] attachment = null)
        {
            var expenseData = new Dictionary<string, object>
            {
                { "employee_id", employeeId },
                { "date", expenseDate.ToString("yyyy-MM-dd") },
                { "name", description },
                { "unit_amount", amount },
                { "product_id", productId }
            };

            if (attachment != null)
            {
                var attachmentData = new Dictionary<string, object>
                {
                    { "name", "Receipt.pdf" },
                    { "datas", Convert.ToBase64String(attachment) },
                    { "res_model", "hr.expense" },
                    { "res_id", 0 }
                };
                var attachmentId = await odooClient.Create("ir.attachment", attachmentData);
                expenseData["attachment_ids"] = new List<long> { attachmentId };
            }

            var expenseId = await odooClient.Create("hr.expense", expenseData);

            if (attachment != null)
            {
                long id;
                // With the following code:
                if (expenseData["attachment_ids"] is List<long> attachmentIds && attachmentIds.Count > 0)
                {
                    id = attachmentIds[0];
                    await odooClient.Update("ir.attachment", id, new Dictionary<string, object> { { "res_id", expenseId } });
                }
                else
                {
                    throw new InvalidOperationException("Attachment IDs are not properly set.");
                }
                await odooClient.Update("ir.attachment", id, new Dictionary<string, object> { { "res_id", expenseId } });
            }
            Console.WriteLine($"Expense mit ID {expenseId} erstellt.");
        }

        public async Task<List<SimpleResult>> GetExpenseProducts()
        {
            var searchParams = new OdooSearchParameters("product.product", new OdooDomainFilter()
                .Filter("can_be_expensed", "=", true));
            var products = await odooClient.Get<SimpleResult[]>(searchParams);
            return products.ToList();
        }

// 5. Belegerfassung für die Finanzbuchhaltung
        public async Task UploadDocumentToAccountMove(long accountMoveId, string documentName, byte[] documentData)
        {
            var attachmentData = new Dictionary<string, object>
            {
                { "name", documentName },
                { "datas", Convert.ToBase64String(documentData) },
                { "res_model", "account.move" },
                { "res_id", accountMoveId }
            };
            var attachmentId = await odooClient.Create("ir.attachment", attachmentData);
            Console.WriteLine($"Dokument mit ID {attachmentId} an account.move {accountMoveId} angehängt.");
        }

// 6. KPIs
        public async Task<ProjectKPI> GetProjectKPI(long projectId)
        {
            var domainFilter = new OdooDomainFilter().Filter("project_id", "=", projectId);
            var searchParams = new OdooSearchParameters("project.project", domainFilter);
            var fieldParams = new OdooFieldParameters(new List<string> { "id", "name", "budget", "total_hours", "billable_hours" });
            var projects = await odooClient.Get<ProjectKPI[]>(searchParams, fieldParams);
            return projects.FirstOrDefault();
        }

        public async Task<double> GetEmployeeUtilization(long employeeId, DateTime startDate, DateTime endDate)
        {
            var attendanceParams = new OdooSearchParameters("hr.attendance", new OdooDomainFilter()
                .Filter("employee_id", "=", employeeId)
                .Filter("check_in", ">=", startDate.ToString("yyyy-MM-dd"))
                .Filter("check_in", "<=", endDate.ToString("yyyy-MM-dd"))
            );
            var attendances = await odooClient.Get<HrAttendance[]>(attendanceParams);
            double totalHours = attendances.Sum(a => (DateTime.Parse(a.check_out) - DateTime.Parse(a.check_in)).TotalHours);

            var timesheetParams = new OdooSearchParameters("account.analytic.line", new OdooDomainFilter()
                .Filter("employee_id", "=", employeeId)
                .Filter("date", ">=", startDate.ToString("yyyy-MM-dd"))
                .Filter("date", "<=", endDate.ToString("yyyy-MM-dd"))
                .Filter("project_id", "!=", false)
            );
            var timesheets = await odooClient.Get<TimesheetResult[]>(timesheetParams);
            double billableHours = timesheets.Sum(t => t.unit_amount);

            return totalHours == 0 ? 0 : billableHours / totalHours;
        }
    }

// Zusätzliche Klassen
    public class HrAttendance
    {
        public string check_in { get; set; }
        public string check_out { get; set; }
    }

    public class TimesheetResult
    {
        public long id { get; set; }
        public string date { get; set; }
        public string description { get; set; } // "name" in Odoo
        public double unit_amount { get; set; }
    }

    public class SimpleResult
    {
        public long id { get; set; }
        public string name { get; set; }
    }

    public class ProjectKPI
    {
        public long id { get; set; }
        public string name { get; set; }
        public double budget { get; set; }
        public double total_hours { get; set; }
        public double billable_hours { get; set; }
    }
}