using UnityEngine;

public class SelectedWallVisual : BaseWall
{
    
    [SerializeField] private GameObject SelectedVisual;
    [SerializeField] private WallMove wallMove;
    private BaseWall baseWall;
    
    private void Awake()
    {
        baseWall = this;
        if (wallMove == null)
        {
            wallMove = GetComponentInParent<WallMove>();
        }
    }
    private void Start()
    {
        
        Player.Instance.OnSelectedWallChanged += Player_OnSelectedWallChanged;
        
    }
    private void OnDestroy()
    {
        Player.Instance.OnSelectedWallChanged -= Player_OnSelectedWallChanged;
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

    public override void Interact(Player player)
    {
        
        wallMove.Interact(player);

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
