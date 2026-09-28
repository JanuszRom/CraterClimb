using UnityEngine;

public class SelectedWallVisual : BaseWall
{
    
    [SerializeField] private GameObject SelectedVisual;
    [SerializeField] private WallMove wallMove;
    //[SerializeField] private Transform wallParent;
    private BaseWall baseWall;
    //private Vector3 startPosition;
    //private Vector3 targetPosition;
    //private bool IsMoving = false;
    //private bool IsMovingBack = false;
    //private Vector3 lastPosition;
    //public Vector3 FrameDelta;
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
        //startPosition = wallParent.position;
        //targetPosition = startPosition + moveDistance;
    }
    private void OnDestroy()
    {
        Player.Instance.OnSelectedWallChanged -= Player_OnSelectedWallChanged;
    }
    //private void Update()
    //{
    //    if (!IsMoving && !IsMovingBack)
    //    {
    //        return;
    //    }
    //    if (IsMoving)
    //    {
    //        Move();
    //    }
    //    else if (IsMovingBack)
    //    {
    //        MoveBack();
    //    }
    //    if (wallParent.position == targetPosition)
    //    {
    //        IsMoving = false;
    //    }
    //    FrameDelta = wallParent.position - lastPosition;
    //    lastPosition = wallParent.position;
    //    //Debug.Log(FrameDelta);
    //}

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
        //if (!IsMoving && !IsMovingBack)
        //{
        //    if (wallParent.position == startPosition)
        //    {
        //        IsMoving = true;
        //    }
        //    else
        //    {
        //        IsMovingBack = true;
        //    }
        //}
        //else if (IsMovingBack)
        //{
        //    IsMovingBack = false;
        //    IsMoving = true;
        //}
        //else
        //{
        //    IsMoving = false;
        //    IsMovingBack = true;
        //}
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
    //private void Move()
    //{
    //    wallParent.position = Vector3.MoveTowards(wallParent.position, targetPosition, moveSpeed * Time.deltaTime);
     
    //}
    //private void MoveBack()
    //{
    //    wallParent.position = Vector3.MoveTowards(wallParent.position, startPosition, moveSpeed * Time.deltaTime);

    //}

}
