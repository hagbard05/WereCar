using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using WearCar.ViewModels;

namespace WearCar.Views
{
	public partial class CompassPage : ContentPage
	{
		readonly CompassViewModel _vm;

			public CompassPage(CompassViewModel vm)
				{
					InitializeComponent();
					BindingContext = _vm = vm;
					_vm.PropertyChanged += Vm_PropertyChanged;

		#if !DEBUG
					SpeedLabel.IsVisible = false;
		#endif

					// initialize visual state
					try
					{
						ArrowContainer.AnchorX = 0.5;
						ArrowContainer.AnchorY = 0.5;
						ArrowContainer.Rotation = _vm.ArrowRotation;
						HeadImage.HeightRequest = _vm.ArrowLength;
					}
					catch { }
				}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			try
			{
				// Ensure the app has background location permission; request if missing
				await _vm.EnsurePermissionsAsync();
			}
			catch { }
		}

		private void CompassArea_SizeChanged(object? sender, EventArgs e)
		{
			try
			{
				if (CompassArea.Width <= 0 || CompassArea.Height <= 0)
					return;

				double availWidth = CompassArea.Width;
				double availHeight = CompassArea.Height;

				// Scale arrow width up to ~55% of available width (minimum 120, maximum 280)
				double arrowWidth = Math.Clamp(availWidth * 0.55, 120.0, 280.0);
				HeadImage.WidthRequest = arrowWidth;

				// Dynamically scale arrow length from 40% of available screen height (close) to 92% (far)
				double minHeight = availHeight * 0.40;
				double maxHeight = availHeight * 0.92;

				_vm.SetScreenHeightBounds(minHeight, maxHeight);
			}
			catch { }
		}

		void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(_vm.ArrowRotation))
			{
				try
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						double target = _vm.ArrowRotation;
						double current = ArrowContainer.Rotation;

						// Calculate shortest path delta in range [-180, 180] degrees
						double delta = (target - (current % 360) + 540) % 360 - 180;
						double targetRotation = current + delta;

						// Cancel any ongoing rotation animation and update rapidly (80ms linear) for instant responsiveness
						ArrowContainer.CancelAnimations();
						ArrowContainer.RotateTo(targetRotation, 80, Easing.Linear);
					});
				}
				catch { }
			}

			if (e.PropertyName == nameof(_vm.ArrowLength))
			{
				try
				{
					double targetHeight = _vm.ArrowLength;

					// animate height smoothly
					var heightAnim = new Microsoft.Maui.Controls.Animation(v => HeadImage.HeightRequest = v, HeadImage.HeightRequest, targetHeight);
					MainThread.BeginInvokeOnMainThread(() => heightAnim.Commit(this, "ArrowHeight", length: 350, easing: Easing.CubicInOut));
				}
				catch { }
			}
		}
	}
}
