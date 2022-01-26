
namespace SpaceCraft
{
    public class ActionExitVehicle :Actionnable
    {
        public ActionEnterVehicle enterVehicleAction;

        		
        public override void OnAction()
		{
            enterVehicleAction.ExitVehicle();
		}

		public override void OnHover()
		{
			Actionnable.hudHandler.DisplayCursorText("Exit", 0f, "Exit SpaceCraft");
			base.OnHover();
		}

		public override void OnHoverOut()
		{
			Actionnable.hudHandler.CleanCursorTextIfCode("Exit");
			base.OnHoverOut();
		}

		private void OnDestroy()
		{
			Actionnable.hudHandler.CleanCursorTextIfCode("Exit");
		}

    }
}