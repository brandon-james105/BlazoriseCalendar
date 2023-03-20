using Blazorise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazoriseCalendarComponent
{
    public partial class Calendar : ComponentBase
    {
        [Inject]
        public IJSRuntime? JSRuntime { get; set; }

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

        private ISet<string> KeysPressed = new HashSet<string>();

        protected override void OnInitialized()
        {
            SelectedDate = DateTime.Today;
            CurrentViewDate = SelectedDate;
            SetMonthView(CurrentViewDate);

            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                foreach (var button in buttonRefs)
                {
                    await JSRuntime?.ThrottleEvent(button.Value.ElementRef, "keydown", TimeSpan.FromMilliseconds(125));
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
            CurrentView = calendarView;
            CurrentViewDate = new DateTime(year, month, day);
            SetDatesInView(CurrentViewDate);

            if (select)
            {
                if (SelectionMode == CalendarSelectionMode.Multiple)
                {
                    SelectedDates.Clear();
                }

                SelectedDate = CurrentViewDate;
                SelectedDates.Add(SelectedDate);
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
                if (!KeysPressed.Contains(args.Key))
                {
                    KeysPressed.Add(args.Key);
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
                        if (KeysPressed.Contains("Control"))
                        {
                            DecrementMonth();
                        }
                        else
                        {
                            CurrentViewDate = date.AddDays(-1);
                        }
                        break;
                    case "ArrowRight":
                        if (KeysPressed.Contains("Control"))
                        {
                            IncrementMonth();
                        }
                        else
                        {
                            CurrentViewDate = date.AddDays(1);
                        }
                        break;
                    case "Enter":
                        SelectedDate = date.Date;
                        break;
                    default:
                        break;
                }
            }
            else if (args.Type == "keyup")
            {
                if (KeysPressed.Contains(args.Key))
                {
                    KeysPressed.Remove(args.Key);
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

        protected void DateClicked(DateTime dateTime)
        {
            if (SelectionMode == CalendarSelectionMode.Single)
            {
                SelectedDate = dateTime;
                CurrentViewDate = SelectedDate;
            }

            if (SelectionMode == CalendarSelectionMode.Multiple)
            {
                if (KeysPressed.Contains("Control"))
                {
                    if (!SelectedDates.Add(dateTime))
                    {
                        SelectedDates.Remove(dateTime);
                    }
                }
                else if (KeysPressed.Contains("Shift"))
                {
                    var dateRange = new List<DateTime>();

                    if (dateTime < SelectedDates.Last())
                    {
                        dateRange = DatesInView.Where(d => d <= SelectedDates.Last() && d >= dateTime).ToList();
                    }
                    else
                    {
                        dateRange = DatesInView.Where(d => d >= SelectedDates.Last() && d <= dateTime).ToList();
                    }

                    foreach (var date in dateRange)
                    {
                        SelectedDates.Add(date);
                    }
                }
                else
                {
                    SelectedDates.Clear();
                    SelectedDates.Add(dateTime);
                }
            }

            SetDatesInView(selectedDate);
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
            string selectedDate = SelectedDate.Equals(date.Date) || SelectedDates.Contains(date.Date) ? "selected-date" : "";
            string focus = CurrentViewDate.Equals(date.Date) ? "focus" : "";

            return string.Join(" ", dateClass, selectedDate, focus);
        }

        private Color DateColor(DateTime date)
        {
            if (SelectionMode == CalendarSelectionMode.Single)
            {
                return SelectedDate.Equals(date.Date) ? Color.Primary : Color.Default;
            }

            if (SelectionMode == CalendarSelectionMode.Multiple || SelectionMode == CalendarSelectionMode.Range)
            {
                return SelectedDates.Contains(date.Date) ? Color.Primary : Color.Default;
            }

            return Color.Default;
            
        }
    }
}
