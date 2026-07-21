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

		void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(_vm.ArrowRotation))
			{
				try
				{
					MainThread.BeginInvokeOnMainThread(async () =>
					{
						// rotate smoothly (degrees)
						await ArrowContainer.RotateTo(_vm.ArrowRotation, 350, Easing.SinInOut);
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
