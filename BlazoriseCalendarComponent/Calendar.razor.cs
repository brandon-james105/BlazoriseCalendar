using Blazorise;
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

        private IList<DateTime> DatesInView { get; set; } = new List<DateTime>();

        private Dictionary<int, Button> buttonRefs = new();

        protected override void OnInitialized()
        {
            SelectedDate = DateTime.Today;
            CurrentViewDate = SelectedDate;
            SetMonthView(CurrentViewDate);

            base.OnInitialized();
        }

        protected void SetMonthView(DateTime dateTime, bool select = false)
        {
            NavigateTo(dateTime, CalendarView.Month, select);
        }

        protected void SetMonthView(int year, int month, bool select = false)
        {
            NavigateTo(year, month, 1, CalendarView.Month, select);
        }

        protected void NavigateTo(DateTime dateTime, CalendarView calendarView = CalendarView.Month, bool select = false)
        {
            NavigateTo(dateTime.Year, dateTime.Month, dateTime.Day, calendarView, select);
        }

        protected void NavigateTo(int year, int month, int day, CalendarView calendarView = CalendarView.Month, bool select = false)
        {
            CurrentView = calendarView;
            CurrentViewDate = new DateTime(year, month, day);
            SetDatesInView(CurrentViewDate);
            
            if (select)
            {
                SelectedDate = CurrentViewDate;
            }

            StateHasChanged();
        }

        protected void DecrementMonth()
        {
            NavigateTo(CurrentViewDate.AddMonths(-1));
        }

        protected bool DecrementMonthEnabled()
        {
            var startOfCurrentMonth = DateTime.Parse($"{CurrentViewDate.Year}-{CurrentViewDate.Month:D2}-01");
            return MinDate < startOfCurrentMonth && MinDate > startOfCurrentMonth.AddMonths(-1) || MinDate == null;
        }

        protected void IncrementMonth()
        {
            NavigateTo(CurrentViewDate.AddMonths(1));
        }

        protected bool IncrementMonthEnabled()
        {
            return MaxDate > DateTime.Parse($"{CurrentViewDate.Year}-{CurrentViewDate.Month:D2}-01").AddMonths(1) || MaxDate == null;
        }

        protected void DecrementYear(int years = 1)
        {
            CurrentViewDate = CurrentViewDate.AddYears(-1 * years);
            SetDatesInView(CurrentViewDate);
        }

        protected void IncrementYear(int years = 1)
        {
            CurrentViewDate = CurrentViewDate.AddYears(years);
            SetDatesInView(CurrentViewDate);
        }

        protected int RoundByX(int val, int x = 10)
        {
            return ((int)Math.Floor(val / (decimal)x)) * x;
        }

        protected async Task HandleDateKeydownAsync(KeyboardEventArgs args, DateTime date)
        {
            switch (args.Key)
            {
                case "ArrowUp":
                    CurrentViewDate = date.AddDays(-7);
                    break;
                case "ArrowDown":
                    CurrentViewDate = date.AddDays(7);
                    break;
                case "ArrowLeft":
                    CurrentViewDate = date.AddDays(-1);
                    break;
                case "ArrowRight":
                    CurrentViewDate = date.AddDays(1);
                    break;
                case "Enter":
                    SelectedDate = date.Date;
                    break;
                default:
                    break;
            }

            if (CurrentViewDate > DateTime.Parse($"{CurrentViewDate.Month}-{CurrentViewDate.Month:D2}-01").AddMonths(1)
                || CurrentViewDate < DateTime.Parse($"{CurrentViewDate.Month}-{CurrentViewDate.Month:D2}-01"))
            {
                SetDatesInView(currentViewDate);
            }

            var index = DatesInView.IndexOf(CurrentViewDate);
            await buttonRefs[index].ElementRef.FocusAsync();

            StateHasChanged();
        }

        private void SetDatesInView(DateTime dateTime)
        {
            var datesInMonth = Enumerable.Range(1, DateTime.DaysInMonth(dateTime.Year, dateTime.Month))
                                         .Select(d => new DateTime(dateTime.Year, dateTime.Month, d))
                                         .ToList();
            var firstDayOfMonth = Array.IndexOf(Enum.GetValues(typeof(DayOfWeek)), datesInMonth.Min().DayOfWeek) + 1;
            var prevDays = Enumerable.Range(1, firstDayOfMonth - 1).Select(d => new DateTime(datesInMonth.Min().AddDays(d - firstDayOfMonth).Ticks));
            var postDays = Enumerable.Range(1, 42 - datesInMonth.Count - prevDays.Count())
                                     .Select(d => new DateTime(datesInMonth.Max().AddDays(d).Ticks));
            
            DatesInView = prevDays.Concat(datesInMonth).Concat(postDays).ToList();
        }

        private string DateClass(DateTime date)
        {
            string dateClass = "calendar-day";
            string selectedDate = SelectedDate.Equals(date.Date) || SelectedDates.Contains(date.Date) ? "selected-date" : "";
            string focus = CurrentViewDate.Equals(date.Date) ? "focus" : "";

            return string.Join(" ", dateClass, selectedDate, focus);
        }
    }
}
