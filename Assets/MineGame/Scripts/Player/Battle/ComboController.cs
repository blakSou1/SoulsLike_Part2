public class ComboController
{
    private ComboModel[] combo;
    public void LoadCombo(ComboModel[] targetCombo) =>
        combo = targetCombo;

    public void DoCombo(AtackInputs inp)
    {
        ComboModel comb = GetComboFromInp(inp);

        if (comb == null)
            return;

        PlayerView.animHook.PlayTargetAnimation(comb.animName, true);
        PlayerView.animHook.canDoCombo = false;
    }
    private ComboModel GetComboFromInp(AtackInputs inp)
    {
        if (combo == null)
            return null;

        for (int i = 0; i < combo.Length; i++)
        {
            if (combo[i].inp == inp)
                return combo[i];
        }

        return null;
    }
}
