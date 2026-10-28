using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

public class ArenaPlayer : NetworkBehaviour
{
    [SerializeField] private Material redMaterial, blueMaterial;
    [SerializeField] private float speed = 7f, jumpSpeed = 9f, gravity = 23f;
    [SerializeField] private float shotRange = 8f, shotHeight = 0.9f, pushForce = 11f, bulletSpeed = 18f;
    [SerializeField] private float fireInterval = 0.4f;
    [Networked] public int Facing { get; set; }
    [Networked] public bool Eliminated { get; set; }
    [Networked] private float VerticalSpeed { get; set; }
    [Networked] private float PushX { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }
    [Networked] private TickTimer FireCooldown { get; set; }
    [Networked] private Vector3 BulletPosition { get; set; }
    [Networked] private float BulletDistance { get; set; }
    [Networked] private int BulletDirection { get; set; }
    [Networked] private bool BulletActive { get; set; }

    private Renderer[] visuals;
    private Transform marker;
    private Transform bullet;
    private Renderer bulletRenderer;
    private const float FootOffset = 0.72f;

    public override void Spawned()
    {
        if (HasStateAuthority) Facing = Object.InputAuthority.RawEncoded == 1 ? 1 : -1;
        visuals = GetComponentsInChildren<Renderer>();
        marker = transform.Find("Direction Marker");
        bullet = transform.Find("Bullet");
        bulletRenderer = bullet.GetComponent<Renderer>();
        foreach (var visual in visuals)
        {
            visual.shadowCastingMode = ShadowCastingMode.Off;
            visual.receiveShadows = false;
            if (visual != bulletRenderer && visual.transform != marker)
                visual.sharedMaterial = Object.InputAuthority.RawEncoded == 1 ? redMaterial : blueMaterial;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || Eliminated) return;
        ArenaMatch match = null;
        foreach (var candidate in FindObjectsByType<ArenaMatch>(FindObjectsSortMode.None))
            if (candidate.Runner == Runner) { match = candidate; break; }
        if (match == null || match.Phase != 2) return;
        GetInput(out ArenaInput input);
        float move = Mathf.Clamp(input.Move, -1f, 1f);
        if (move != 0f) Facing = move > 0 ? 1 : -1;

        bool grounded = false;
        foreach (var platform in FindObjectsByType<ArenaPlatform>(FindObjectsSortMode.None))
        {
            var b = platform.Bounds;
            if (transform.position.x >= b.min.x - 0.3f && transform.position.x <= b.max.x + 0.3f &&
                Mathf.Abs(transform.position.y - FootOffset - b.max.y) < 0.08f) grounded = true;
        }
        if (grounded && input.Buttons.WasPressed(PreviousButtons, ArenaButton.Jump))
            VerticalSpeed = jumpSpeed;
        PreviousButtons = input.Buttons;

        if (input.Buttons.IsSet(ArenaButton.Fire) && !BulletActive && FireCooldown.ExpiredOrNotRunning(Runner))
        {
            FireCooldown = TickTimer.CreateFromSeconds(Runner, fireInterval);
            BulletPosition = transform.position + new Vector3(Facing * 0.55f, 0.25f, -2.5f);
            BulletDirection = Facing;
            BulletDistance = 0f;
            BulletActive = true;
        }

        float dt = Runner.DeltaTime;
        if (BulletActive)
        {
            float oldX = BulletPosition.x;
            BulletPosition += Vector3.right * (BulletDirection * bulletSpeed * dt);
            BulletDistance += bulletSpeed * dt;
            var other = Object == match.RedPlayer ? match.BluePlayer : match.RedPlayer;
            var target = other != null ? other.GetComponent<ArenaPlayer>() : null;
            if (target != null && !target.Eliminated &&
                target.transform.position.x >= Mathf.Min(oldX, BulletPosition.x) - 0.3f &&
                target.transform.position.x <= Mathf.Max(oldX, BulletPosition.x) + 0.3f &&
                Mathf.Abs(target.transform.position.y - BulletPosition.y) < shotHeight)
            {
                target.Knockback(BulletDirection, pushForce);
                BulletActive = false;
            }
            if (BulletDistance >= shotRange) BulletActive = false;
        }
        var oldPosition = transform.position;
        VerticalSpeed -= gravity * dt;
        var position = oldPosition + new Vector3(move * speed + PushX, VerticalSpeed, 0f) * dt;
        PushX = Mathf.MoveTowards(PushX, 0f, 18f * dt);
        if (VerticalSpeed <= 0f)
            foreach (var platform in FindObjectsByType<ArenaPlatform>(FindObjectsSortMode.None))
            {
                var b = platform.Bounds;
                if (position.x < b.min.x - 0.3f || position.x > b.max.x + 0.3f) continue;
                if (oldPosition.y - FootOffset >= b.max.y - 0.08f && position.y - FootOffset <= b.max.y)
                {
                    position.y = b.max.y + FootOffset;
                    VerticalSpeed = 0f;
                }
            }
        transform.position = position;
    }

    public override void Render()
    {
        var peer = Runner.GetComponent<ArenaLauncher>();
        bool visible = !Eliminated && (peer == null || !peer.HideNetworkVisuals);
        foreach (var visual in visuals) visual.enabled = visible;
        if (marker != null) marker.localPosition = new Vector3(Facing * Mathf.Abs(marker.localPosition.x), marker.localPosition.y, marker.localPosition.z);
        if (bullet != null) bullet.position = BulletPosition;
        if (bulletRenderer != null) bulletRenderer.enabled = visible && BulletActive;
    }

    public void Knockback(int direction, float force)
    {
        if (!HasStateAuthority) return;
        PushX = direction * force;
        VerticalSpeed = 6f;
    }

    public void SetEliminated(bool value) { if (HasStateAuthority) Eliminated = value; }

    public void ResetForRound(Vector3 position)
    {
        if (!HasStateAuthority) return;
        transform.position = position;
        VerticalSpeed = PushX = 0f;
        BulletActive = false;
        Facing = position.x < 0f ? 1 : -1;
        Eliminated = false;
    }
}
