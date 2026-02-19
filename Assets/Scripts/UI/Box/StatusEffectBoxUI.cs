using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상태 이상 박스 UI - 버프/디버프 표시
/// </summary>
public class StatusEffectBoxUI : UI_Base
{
	private enum Images { EffectIcon, }
	private enum Texts { EffectName, RemainingTime, }

	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		BindImage(typeof(Images));
		BindText(typeof(Texts));

		return true;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	public void UpdateData(string effectName, Sprite effectIcon)
	{
		var iconImage = GetImage((int)Images.EffectIcon);
		if (iconImage != null)
		{
			iconImage.sprite = effectIcon;
		}

		var nameText = GetText((int)Texts.EffectName);
		if (nameText != null)
		{
			nameText.text = effectName;
		}
	}

	public void UpdateRemainingTime(int remainingTurns)
	{
		var timeText = GetText((int)Texts.RemainingTime);
		if (timeText != null)
		{
			timeText.text = remainingTurns.ToString();
		}
	}
}
