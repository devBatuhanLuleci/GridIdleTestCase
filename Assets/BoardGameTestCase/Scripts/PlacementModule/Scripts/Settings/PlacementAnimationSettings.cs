using UnityEngine;
using DG.Tweening;

namespace PlacementModule.Settings
{
	[CreateAssetMenu(fileName = "PlacementAnimationSettings", menuName = "Placement/Placement Animation Settings", order = 0)]
	public class PlacementAnimationSettings : ScriptableObject
	{		public bool enablePositionTween = true;		public float positionDuration = 0.15f;
		
		[Tooltip("Easing type for position animation (e.g., OutBack, InOutQuad)")]
		public Ease positionEase = Ease.OutBack;		public float positionOvershoot = 1.1f;

		[Header("Position Tween (Invalid -> Snap)")]		public bool useSeparateInvalidSnap = true;
		
		[Tooltip("Duration when snapping from invalid position (usually faster)")]
		public float invalidSnapDuration = 0.08f;		public Ease invalidSnapEase = Ease.OutCubic;		public float invalidSnapOvershoot = 1.0f;

		[Header("Scale Punch (optional)")]		public bool enableScalePunch = true;
		
		[Tooltip("Scale amount for punch effect (X, Y, Z)")]
		public Vector3 punchScale = new Vector3(0.1f, 0.1f, 0f);		public float punchDuration = 0.2f;		public int punchVibrato = 8;
		
		[Tooltip("Elasticity of the punch effect (0-1, higher = more bouncy)")]
		public float punchElasticity = 1f;		public bool enableSwapAnimation = true;		public float swapDuration = 0.2f;		public Ease swapEase = Ease.InOutQuad;		public float swapOvershoot = 1.0f;		public bool enableSwapScalePunch = true;
		
		[Tooltip("Scale amount for swap punch effect (X, Y, Z)")]
		public Vector3 swapPunchScale = new Vector3(0.12f, 0.12f, 0f);		public float swapPunchDuration = 0.25f;		public int swapPunchVibrato = 10;
		
		[Tooltip("Elasticity of swap punch effect (0-1, higher = more bouncy)")]
		public float swapPunchElasticity = 0.8f;

		public static PlacementAnimationSettings LoadOrDefaults()
		{
			var settings = Resources.Load<PlacementAnimationSettings>("Placement/PlacementAnimationSettings");
			if (settings != null) return settings;

			var temp = CreateInstance<PlacementAnimationSettings>();
			temp.enablePositionTween = true;
			temp.positionDuration = 0.15f;
			temp.positionEase = Ease.OutBack;
			temp.positionOvershoot = 1.1f;
			temp.useSeparateInvalidSnap = true;
			temp.invalidSnapDuration = 0.08f;
			temp.invalidSnapEase = Ease.OutCubic;
			temp.invalidSnapOvershoot = 1.0f;
			temp.enableScalePunch = true;
			temp.punchScale = new Vector3(0.1f, 0.1f, 0f);
			temp.punchDuration = 0.2f;
			temp.punchVibrato = 8;
			temp.punchElasticity = 1f;
			temp.enableSwapAnimation = true;
			temp.swapDuration = 0.2f;
			temp.swapEase = Ease.InOutQuad;
			temp.swapOvershoot = 1.0f;
			temp.enableSwapScalePunch = true;
			temp.swapPunchScale = new Vector3(0.12f, 0.12f, 0f);
			temp.swapPunchDuration = 0.25f;
			temp.swapPunchVibrato = 10;
			temp.swapPunchElasticity = 0.8f;
			return temp;
		}
	}
}

