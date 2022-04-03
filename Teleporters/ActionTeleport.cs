using MijuTools;
using SpaceCraft;

public class ActionTeleport : Actionnable
{
    public override void OnAction()
    {
        Managers.GetManager<WindowsHandler>().OpenAndReturnUi((DataConfig.UiType) UiWindowTeleport.UI_WINDOW_ID);
    }

    public override void OnHover()
    {
        Actionnable.hudHandler.DisplayCursorText("Teleport", 0f, "Teleport");
        base.OnHover();
    }

    public override void OnHoverOut()
    {
        Actionnable.hudHandler.CleanCursorTextIfCode("Teleport");
        base.OnHoverOut();
    }

    private void OnDestroy()
    {
        Actionnable.hudHandler.CleanCursorTextIfCode("Teleport");
    }
}