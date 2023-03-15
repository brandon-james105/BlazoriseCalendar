using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazoriseCalendarComponent
{
    public partial class Calendar : ComponentBase
    {
        private DateTime selectedDate;

        [Parameter]
        public DateTime SelectedDate
        {
            get => selectedDate;
            set
            {
                if (selectedDate == value) return;
                selectedDate = value;
                SelectedDateChanged?.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<DateTime>? SelectedDateChanged { get; set; }

        private DateTime currentViewDate;

        [Parameter]
        public DateTime CurrentViewDate
        {
            get => currentViewDate;
            set
            {
                if (currentViewDate == value) return;
                currentViewDate = value;
                CurrentViewDateChanged?.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<DateTime>? CurrentViewDateChanged { get; set; }

        private ISet<DateTime> disabledDates = new HashSet<DateTime>();

        [Parameter]
        public IEnumerable<DateTime> DisabledDates
        {
            get => disabledDates;
            set => disabledDates = value.Select(v => v.Date).ToHashSet();
        }

        [Parameter]
        public DateTime? MinDate { get; set; }

        [Parameter]
        public DateTime? MaxDate { get; set; }

        [Parameter]
        public CalendarSelectionMode SelectionMode { get; set; }

        protected CalendarView CurrentView { get; set; }

        private ISet<DateTime> SelectedDates { get; set; } = new HashSet<DateTime>();

        private IList<DateTime> DatesInMonth { get; set; } = new List<DateTime>();

        protected override void OnInitialized()
        {
            SelectedDate = DateTime.Today;
            CurrentViewDate = SelectedDate;
            SetMonthView(CurrentViewDate);

            base.OnInitialized();
        }

        protected void SetMonthView(DateTime dateTime)
        {
            SetMonthView(dateTime.Year, dateTime.Month);
        }

        protected void SetMonthView(int year, int month)
        {
            NavigateTo(year, month, 1, CalendarView.Month);
        }

        protected void NavigateTo(DateTime dateTime, CalendarView calendarView = CalendarView.Month)
        {
            NavigateTo(dateTime.Year, dateTime.Month, dateTime.Day);
            CurrentView = calendarView;
        }

        protected void NavigateTo(int year, int month, int day, CalendarView calendarView = CalendarView.Month)
        {
            CurrentViewDate = new DateTime(year, month, day);
            CalculateDateValues(CurrentViewDate);
            CurrentView = calendarView;
        }

        protected void DecrementMonth()
        {
            NavigateTo(CurrentViewDate.Month > 1 ? CurrentViewDate.Year : CurrentViewDate.Year - 1, CurrentViewDate.Month == 1 ? 12 : CurrentViewDate.Month - 1, 1);
        }

        protected bool DecrementMonthEnabled()
        {
            var startOfCurrentMonth = DateTime.Parse($"{CurrentViewDate.Year}-{CurrentViewDate.Month:D2}-01");
            return MinDate < startOfCurrentMonth && MinDate > startOfCurrentMonth.AddMonths(-1) || MinDate == null;
        }

        protected void IncrementMonth()
        {
            NavigateTo(CurrentViewDate.Month < 12 ? CurrentViewDate.Year : CurrentViewDate.Year + 1, CurrentViewDate.Month == 12 ? 1 : CurrentViewDate.Month + 1, 1);
        }

        protected bool IncrementMonthEnabled()
        {
            return MaxDate > DateTime.Parse($"{CurrentViewDate.Year}-{CurrentViewDate.Month:D2}-01").AddMonths(1) || MaxDate == null;
        }

        protected void DecrementYear(int years = 1)
        {
            CurrentViewDate = CurrentViewDate.AddYears(-1 * years);
            CalculateDateValues(CurrentViewDate);
        }

        protected void IncrementYear(int years = 1)
        {
            CurrentViewDate = CurrentViewDate.AddYears(years);
            CalculateDateValues(CurrentViewDate);
        }

        protected int RoundByX(int val, int x = 10)
        {
            return ((int)Math.Floor(val / (decimal)x)) * x;
        }

        private void CalculateDateValues(DateTime dateTime)
        {
            var datesInMonth = Enumerable.Range(1, DateTime.DaysInMonth(dateTime.Year, dateTime.Month))
                                         .Select(d => new DateTime(dateTime.Year, dateTime.Month, d))
                                         .ToList();
            var firstDayOfMonth = Array.IndexOf(Enum.GetValues(typeof(DayOfWeek)), datesInMonth.Min().DayOfWeek) + 1;
            var prevDays = Enumerable.Range(1, firstDayOfMonth - 1).Select(d => new DateTime(datesInMonth.Min().AddDays(d - firstDayOfMonth).Ticks));
            var postDays = Enumerable.Range(1, 42 - datesInMonth.Count - prevDays.Count())
                                     .Select(d => new DateTime(datesInMonth.Max().AddDays(d).Ticks));
            DatesInMonth = prevDays.Concat(datesInMonth).Concat(postDays).ToList();
        }
    }
}
