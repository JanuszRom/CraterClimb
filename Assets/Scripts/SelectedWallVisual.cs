using UnityEngine;

public class SelectedWallVisual : BaseWall
{
    
    [SerializeField] private GameObject SelectedVisual;
    [SerializeField] private Transform wallParent;
    private BaseWall baseWall;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool IsMoving = false;
    private void Awake()
    {
        baseWall = this;
       }
    private void Start()
    {
        
        Player.Instance.OnSelectedWallChanged += Player_OnSelectedWallChanged;
        startPosition = wallParent.position;
        targetPosition = startPosition + moveDistance;
    }
    private void Update()
    {
        if (!IsMoving)
        {
            return;
        }
            Move();
        if (wallParent.position == targetPosition)
        {
            IsMoving = false;
        }
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
        IsMoving = true;
        Debug.Log("Interacting with SelectedWallVisual");
    }

    private void Show()
    {
        SelectedVisual.SetActive(true);
    }
    private void Hide()
    {
        SelectedVisual.SetActive(false);
    }
    private void Move()
    {
        wallParent.position = Vector3.MoveTowards(wallParent.position, targetPosition, moveSpeed * Time.deltaTime);
     
    }
}
