namespace WearCar
{
	public partial class MainPage : ContentPage
	{
		int count = 0;

		public MainPage()
		{
			InitializeComponent();
		}

		private async void OnOpenParkedMapClicked(object? sender, EventArgs e)
		{
			try
			{
				var services = Application.Current?.Handler?.MauiContext?.Services;
				if (services == null)
				{
					await DisplayAlertAsync("Error", "Services are not available.", "OK");
					return;
				}

				var page = services.GetService<WearCar.Views.ParkedMapPage>();
				if (page == null)
				{
					await DisplayAlertAsync("Error", "Parked Map page is not registered.", "OK");
					return;
				}

				await Shell.Current.Navigation.PushAsync(page);
			}
			catch (Exception ex)
			{
				await DisplayAlertAsync("Error", ex.Message, "OK");
			}
		}

		private void OnCounterClicked(object? sender, EventArgs e)
		{
			count++;

			if (count == 1)
				CounterBtn.Text = $"Clicked {count} time";
			else
				CounterBtn.Text = $"Clicked {count} times";

			SemanticScreenReader.Announce(CounterBtn.Text);
		}
	}
}
