using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;

namespace Riverside.MotionTracking.BodySensor;

[Activity(Label = "Watch Tracker", MainLauncher = true)]
public class MainActivity : Activity
{
	private const int RequestBodySensors = 100;

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		SetContentView(Resource.Layout.activity_main);

		if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
			CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Permission.Granted)
		{
			RequestPermissions([Android.Manifest.Permission.PostNotifications], RequestBodySensors + 1);
		}

		Button startButton = FindViewById<Button>(Resource.Id.startButton)!;
		startButton.Click += (_, _) => OnStartClicked();
	}

	private void OnStartClicked()
	{
		if (CheckSelfPermission(Android.Manifest.Permission.BodySensors) == Permission.Granted)
		{
			StartTrackerService();
		}
		else
		{
			RequestPermissions([Android.Manifest.Permission.BodySensors], RequestBodySensors);
		}
	}

	public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
	{
		base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

		if (requestCode == RequestBodySensors && grantResults.Length > 0 && grantResults[0] == Permission.Granted)
		{
			StartTrackerService();
		}
		else if (requestCode == RequestBodySensors)
		{
			Toast.MakeText(this, "Body sensors permission is required for tracking", ToastLength.Long)?.Show();
		}
	}

	private void StartTrackerService()
	{
		EditText ipEditText = FindViewById<EditText>(Resource.Id.targetIpEditText)!;
		string? targetIp = ipEditText.Text?.Trim();

		Intent serviceIntent = new(this, typeof(MotionTrackingService));
		if (!string.IsNullOrWhiteSpace(targetIp))
		{
			serviceIntent.PutExtra(MotionTrackingService.ExtraTargetIp, targetIp);
		}

		StartForegroundService(serviceIntent);
	}
}
