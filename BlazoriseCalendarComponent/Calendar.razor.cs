using Blazorise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BlazoriseCalendarComponent
{
    public partial class Calendar : ComponentBase
    {
        /// <summary>
        /// Keeps track of how many controls are in play across the instances of calendar components
        /// </summary>
        private static int controlCount = 0;

        [Inject]
        public IJSRuntime? JSRuntime { get; set; }

        private DateTime? selectedDate;

        [Parameter]
        public DateTime? Date
        {
            get => selectedDate;
            set
            {
                if (selectedDate == value) return;
                selectedDate = value;
                DateChanged?.InvokeAsync(value);
                ValueChanged?.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<DateTime?>? DateChanged { get; set; }

        [Parameter]
        public EventCallback<DateTime?>? ValueChanged { get; set; }

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
                StateHasChanged();
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

        private CalendarSelectionMode selectionMode;

        [Parameter]
        public CalendarSelectionMode SelectionMode
        {
            get => selectionMode;
            set
            {
                selectionMode = value;

                SelectedDates.Clear();
                RangeStart = null;
                RangeEnd = null;
                rangeSelectHoveredDate = null;
                Date = null;
                StateHasChanged();
            }
        }

        [Parameter]
        public CalendarView View { get; set; }

        private DateTime? rangeStart;

        [Parameter]
        public DateTime? RangeStart
        {
            get => rangeStart;
            set
            {
                if (rangeStart != value)
                {
                    rangeStart = value;
                    RangeStartChanged?.InvokeAsync(value);
                    StateHasChanged();
                }
            }
        }

        [Parameter]
        public EventCallback<DateTime?>? RangeStartChanged { get; set; }

        private DateTime? rangeEnd;

        [Parameter]
        public DateTime? RangeEnd
        {
            get => rangeEnd;
            set
            {
                if (rangeEnd != value)
                {
                    rangeEnd = value;
                    RangeEndChanged?.InvokeAsync(value);
                    StateHasChanged();
                }
            }
        }

        [Parameter]
        public EventCallback<DateTime?>? RangeEndChanged { get; set; }

        [Parameter]
        public int Views
        {
            get => views;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                views = value;
            }
        }

        [Parameter]
        public Orientation Orientation { get; set; }

        protected DateTime? rangeSelectHoveredDate;

        private ISet<DateTime> SelectedDates { get; set; } = new HashSet<DateTime>();

        private IList<DateTime> DatesInView { get; set; } = new List<DateTime>();

        private readonly IDictionary<int, Button> buttonRefs = new Dictionary<int, Button>();

        private readonly ISet<string> keysPressed = new HashSet<string>();

        private int views = 1;

        private const int totalDatesInView = 42;

        protected override async Task OnInitializedAsync()
        {
            CurrentViewDate = DateTime.Today;
            await SetMonthView(CurrentViewDate);

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (JSRuntime != null)
                {
                    foreach (var button in buttonRefs)
                    {
                        await JSRuntime.ThrottleEvent(button.Value.ElementRef, "keydown", TimeSpan.FromMilliseconds(125));
                    }
                }
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        protected async Task SetMonthView(DateTime dateTime, bool select = false)
        {
            await NavigateTo(dateTime, CalendarView.Month, select);
        }

        protected async Task SetMonthView(int year, int month, bool select = false)
        {
            await NavigateTo(year, month, 1, CalendarView.Month, select);
        }

        protected async Task NavigateTo(int year, int month, int day, CalendarView calendarView = CalendarView.Month, bool select = false)
        {
            var dateTime = new DateTime(year, month, day);
            await NavigateTo(dateTime, calendarView, select);
        }

        protected async Task NavigateTo(DateTime dateTime, CalendarView calendarView = CalendarView.Month, bool select = false)
        {
            View = calendarView;

            SetDatesInView(dateTime);

            if (select)
            {
                await HandleSelection(dateTime);
            }
        }

        protected async Task DecrementMonth()
        {
            await NavigateTo(CurrentViewDate.AddMonths(-views));
        }

        protected bool DecrementMonthEnabled()
        {
            var startOfCurrentMonth = DateTime.Parse($"{CurrentViewDate.Year}-{CurrentViewDate.Month:D2}-01");
            return MinDate < startOfCurrentMonth && MinDate > startOfCurrentMonth.AddMonths(-1) || MinDate == null;
        }

        protected async Task IncrementMonth()
        {
            await NavigateTo(CurrentViewDate.AddMonths(views));
        }

        protected bool IncrementMonthEnabled()
        {
            return MaxDate > DateTime.Parse($"{CurrentViewDate.Year}-{CurrentViewDate.Month:D2}-01").AddMonths(views) || MaxDate == null;
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

        protected void HandleDateMouseEnter(MouseEventArgs args, DateTime date)
        {
            if (SelectionMode == CalendarSelectionMode.Range)
            {
                rangeSelectHoveredDate = date;
            }
        }

        protected void HandleDateMouseLeave(MouseEventArgs args, DateTime date)
        {
            if (SelectionMode != CalendarSelectionMode.Range)
            {
                rangeSelectHoveredDate = null;
            }
        }

        protected async Task HandleDateKeypressAsync(KeyboardEventArgs args, DateTime date)
        {
            var repeatableKeys = new List<string>()
            {
                "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"
            };

            if (args.Repeat && !repeatableKeys.Contains(args.Key))
            {
                return;
            }

            if (args.Type == "keydown")
            {
                if (!keysPressed.Contains(args.Key))
                {
                    keysPressed.Add(args.Key);
                }

                switch (args.Key)
                {
                    case "ArrowUp":
                        CurrentViewDate = date.AddDays(-7);
                        break;
                    case "ArrowDown":
                        CurrentViewDate = date.AddDays(7);
                        break;
                    case "ArrowLeft":
                        if (keysPressed.Contains("Control"))
                        {
                            await DecrementMonth();
                        }
                        else
                        {
                            CurrentViewDate = date.AddDays(-1);
                        }
                        break;
                    case "ArrowRight":
                        if (keysPressed.Contains("Control"))
                        {
                            await IncrementMonth();
                        }
                        else
                        {
                            CurrentViewDate = date.AddDays(1);
                        }
                        break;
                    case "Enter":
                        await HandleSelection(date);
                        break;
                    default:
                        break;
                }
            }
            else if (args.Type == "keyup")
            {
                if (keysPressed.Contains(args.Key))
                {
                    keysPressed.Remove(args.Key);
                }
            }

            if (CurrentViewDate > DateTime.Parse($"{CurrentViewDate.Month}-{CurrentViewDate.Month:D2}-01").AddMonths(views)
                || CurrentViewDate < DateTime.Parse($"{CurrentViewDate.Month}-{CurrentViewDate.Month:D2}-01"))
            {
                SetDatesInView(currentViewDate);
            }

            var index = DatesInView.IndexOf(CurrentViewDate);
            await buttonRefs[index].ElementRef.FocusAsync();

            StateHasChanged();
        }

        protected async Task DateClicked(DateTime date)
        {
            SetDatesInView(date);
            await HandleSelection(date);
        }

        private async Task HandleSelection(DateTime date)
        {
            if (views == 1)
            {
                CurrentViewDate = date;
            }

            if (SelectionMode == CalendarSelectionMode.Single)
            {
                Date = date;
            }
            if (SelectionMode == CalendarSelectionMode.Multiple)
            {
                if (keysPressed.Contains("Control"))
                {
                    if (!SelectedDates.Add(date))
                    {
                        SelectedDates.Remove(date);
                    }
                }
                else if (keysPressed.Contains("Shift"))
                {
                    var dateRange = new List<DateTime>();

                    if (date < SelectedDates.Last())
                    {
                        dateRange = DatesInView.Where(d => d <= SelectedDates.Last() && d >= date).ToList();
                    }
                    else
                    {
                        dateRange = DatesInView.Where(d => d >= SelectedDates.Last() && d <= date).ToList();
                    }

                    foreach (var d in dateRange)
                    {
                        SelectedDates.Add(d);
                    }
                }
                else
                {
                    SelectedDates.Clear();
                    SelectedDates.Add(date);
                }
            }
            if (SelectionMode == CalendarSelectionMode.Range)
            {
                if (RangeStart == null)
                {
                    RangeStart = date;
                }
                else if (RangeStart != null && RangeEnd != null)
                {
                    if (date < RangeEnd && date > RangeStart)
                    {
                        RangeStart = date;
                    }
                    else
                    {
                        RangeStart = date;
                        RangeEnd = null;
                    }
                }
                else if (RangeStart != null)
                {
                    if (date < RangeStart)
                    {
                        RangeStart = date;
                    }
                    else if (date > RangeStart)
                    {
                        RangeEnd = date;
                    }
                }
            }

            ValueChanged?.InvokeAsync(date);

            var index = DatesInView.IndexOf(date);

            if (index != -1)
            {
                await buttonRefs[index].ElementRef.FocusAsync();
            }
        }

        private void SetDatesInView(DateTime dateTime)
        {
            var startDate = dateTime;

            if (DatesInView.Any())
            {
                var firstDateInView = DatesInView.Min();
                var firstDayOfMonthOffset = 0;

                while (firstDateInView.AddDays(firstDayOfMonthOffset).Day != 1)
                {
                    firstDayOfMonthOffset++;
                }

                var firstDayOfMonthInView = firstDateInView.AddDays(firstDayOfMonthOffset);
                var endOfLastMonthInView = firstDayOfMonthInView.AddMonths(views);

                // If the navigated date is before the start of the first month, subtract the amount of views as months.
                if (startDate < firstDayOfMonthInView && startDate >= DatesInView.Min())
                {
                    startDate = startDate.AddMonths(-views);
                }
                // If the navigated date is after the end of the last month, add the amount of views as month.
                if (startDate > endOfLastMonthInView && startDate <= DatesInView.Max())
                {
                    startDate = startDate.AddMonths(views);
                }

                if (startDate > firstDayOfMonthInView && startDate < endOfLastMonthInView)
                {
                    CurrentViewDate = startDate;
                    return;
                }
            }

            var datesToSet = new List<DateTime>();

            for (int i = 0; i < views; i++)
            {
                var viewMonth = startDate.AddMonths(i).Month;
                var viewYear = startDate.AddMonths(i).Year;
                var viewDatesInMonth = Enumerable.Range(1, DateTime.DaysInMonth(viewYear, viewMonth))
                                                 .Select(d => new DateTime(viewYear, viewMonth, d));
                var viewFirstDayOfMonth = Array.IndexOf(Enum.GetValues(typeof(DayOfWeek)), viewDatesInMonth.Min().DayOfWeek) + 1;
                var viewPrevDays = Enumerable.Range(1, viewFirstDayOfMonth - 1)
                                             .Select(d => viewDatesInMonth.Min().AddDays(d - viewFirstDayOfMonth));
                var viewPostDays = Enumerable.Range(1, totalDatesInView - viewDatesInMonth.Count() - viewPrevDays.Count())
                                             .Select(d => viewDatesInMonth.Max().AddDays(d));

                datesToSet = datesToSet.Concat(viewPrevDays.Concat(viewDatesInMonth).Concat(viewPostDays)).ToList();
            }

            DatesInView = datesToSet;

            CurrentViewDate = dateTime;

            StateHasChanged();
        }

        private IFluentFlex? CalendarFlex()
        {
            if (views == 1)
            {
                return null;
            }

            return Orientation == Orientation.Horizontal ? Flex.Row : Flex.Column;
        }

        private string DateClass(DateTime date)
        {
            string dateClass = "calendar-day";
            string selectedDate = RangeStart == date.Date || RangeEnd == date.Date
                                    || Date.Equals(date.Date)
                                    || SelectedDates.Contains(date.Date) ? "selected-date" : "";
            string focus = CurrentViewDate.Equals(date.Date) ? "focus" : "";
            string inRange = "";

            if (SelectionMode == CalendarSelectionMode.Range)
            {
                if (RangeStart != null && RangeEnd != null)
                {
                    inRange = date > RangeStart && date < RangeEnd ? "in-range" : "";
                }

                if (RangeStart != null && RangeEnd == null)
                {
                    inRange = date > RangeStart && date < rangeSelectHoveredDate ? "in-range" : "";
                }
            }

            var classes = new HashSet<string>() { dateClass, selectedDate, focus, inRange };

            return string.Join(" ", classes.Where(c => !string.IsNullOrEmpty(c)));
        }

        private TextColor DateTextColor(DateTime date, int view)
        {
            return CurrentViewDate.AddMonths(view).Month == date.Month ? TextColor.Default : TextColor.Muted;
        }

        private Color DateColor(DateTime date)
        {
            if (SelectionMode == CalendarSelectionMode.Single)
            {
                return Date.Equals(date.Date) ? Color.Primary : Color.Default;
            }

            if (SelectionMode == CalendarSelectionMode.Multiple)
            {
                return SelectedDates.Contains(date.Date) ? Color.Primary : Color.Default;
            }

            if (SelectionMode == CalendarSelectionMode.Range)
            {
                return RangeStart == date || RangeEnd == date ? Color.Primary : Color.Default;
            }

            return Color.Default;
        }
    }
}
