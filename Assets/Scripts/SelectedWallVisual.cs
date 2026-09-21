using UnityEngine;

public class SelectedWallVisual : BaseWall
{
    [SerializeField] private BaseWall baseWall;
    [SerializeField] private GameObject SelectedVisual;

    private void Awake()
    {
        baseWall = this;
    }
    private void Start()
    {
        
        Player.Instance.OnSelectedWallChanged += Player_OnSelectedWallChanged;
        
    }

    private void Player_OnSelectedWallChanged(object sender, Player.OnSelectedWallChangedEventArgs e)
    {
   
        if (e.selectedWall == baseWall)
        {
            
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void Show()
    {
        SelectedVisual.SetActive(true);
    }
    private void Hide()
    {
        SelectedVisual.SetActive(false);
    }
}
