using Blazorise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

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
                RangeSelectHoveredDate = null;
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

        protected DateTime? RangeSelectHoveredDate;

        private ISet<DateTime> SelectedDates { get; set; } = new HashSet<DateTime>();

        private IList<DateTime> DatesInView { get; set; } = new List<DateTime>();

        private Dictionary<int, Button> buttonRefs = new();

        private ISet<string> keysPressed = new HashSet<string>();

        protected override void OnInitialized()
        {
            CurrentViewDate = DateTime.Today;
            SetMonthView(CurrentViewDate);

            base.OnInitialized();
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
            View = calendarView;
            CurrentViewDate = new DateTime(year, month, day);
            SetDatesInView(CurrentViewDate);

            if (select)
            {
                if (SelectionMode == CalendarSelectionMode.Multiple)
                {
                    SelectedDates.Clear();
                }

                Date = CurrentViewDate;
                SelectedDates.Add(Date.Value);
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

        protected void HandleDateMouseEnter(MouseEventArgs args, DateTime date)
        {
            if (SelectionMode == CalendarSelectionMode.Range)
            {
                RangeSelectHoveredDate = date;
            }
        }

        protected void HandleDateMouseLeave(MouseEventArgs args, DateTime date)
        {
            if (SelectionMode != CalendarSelectionMode.Range)
            {
                RangeSelectHoveredDate = null;
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
                            DecrementMonth();
                        }
                        else
                        {
                            CurrentViewDate = date.AddDays(-1);
                        }
                        break;
                    case "ArrowRight":
                        if (keysPressed.Contains("Control"))
                        {
                            IncrementMonth();
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

            if (CurrentViewDate > DateTime.Parse($"{CurrentViewDate.Month}-{CurrentViewDate.Month:D2}-01").AddMonths(1)
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
            CurrentViewDate = date;

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

            var index = DatesInView.IndexOf(CurrentViewDate);
            await buttonRefs[index].ElementRef.FocusAsync();
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

            var datesToSet = prevDays.Concat(datesInMonth).Concat(postDays).ToList();

            if (!DatesInView.SequenceEqual(datesToSet))
            {
                DatesInView = datesToSet;
            }
            StateHasChanged();
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
                    inRange = date > RangeStart && date < RangeSelectHoveredDate ? "in-range" : "";
                }
            }

            ISet<string> classes = new HashSet<string>() { dateClass, selectedDate, focus, inRange };

            return string.Join(" ", classes.Where(c => !string.IsNullOrEmpty(c)));
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
