using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

public class ArenaPlayer : NetworkBehaviour
{
    [SerializeField] private Material redMaterial, blueMaterial;
    [SerializeField] private float speed = 7f, jumpSpeed = 9f, gravity = 23f;
    [SerializeField] private float shotRange = 8f, shotHeight = 0.9f, pushForce = 11f;
    [SerializeField] private float fireInterval = 0.4f;
    [Networked] public int Facing { get; set; }
    [Networked] public bool Eliminated { get; set; }
    [Networked] private float VerticalSpeed { get; set; }
    [Networked] private float PushX { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }
    [Networked] private TickTimer FireCooldown { get; set; }
    [Networked] private TickTimer Flash { get; set; }

    private Renderer[] visuals;
    private Transform marker;
    private LineRenderer beam;

    public override void Spawned()
    {
        if (HasStateAuthority) Facing = Object.InputAuthority.RawEncoded == 1 ? 1 : -1;
        visuals = GetComponentsInChildren<Renderer>();
        marker = transform.Find("Direction Marker");
        beam = GetComponentInChildren<LineRenderer>();
        foreach (var visual in visuals)
        {
            visual.shadowCastingMode = ShadowCastingMode.Off;
            visual.receiveShadows = false;
            if (visual != beam && visual.transform != marker)
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
                Mathf.Abs(transform.position.y - 1f - b.max.y) < 0.08f) grounded = true;
        }
        if (grounded && input.Buttons.WasPressed(PreviousButtons, ArenaButton.Jump))
            VerticalSpeed = jumpSpeed;
        PreviousButtons = input.Buttons;

        if (input.Buttons.IsSet(ArenaButton.Fire) && FireCooldown.ExpiredOrNotRunning(Runner))
        {
            FireCooldown = TickTimer.CreateFromSeconds(Runner, fireInterval);
            Flash = TickTimer.CreateFromSeconds(Runner, 0.13f);
            foreach (var player in Runner.ActivePlayers)
            {
                if (player == Object.InputAuthority) continue;
                var target = Runner.GetPlayerObject(player)?.GetComponent<ArenaPlayer>();
                if (target == null || target.Eliminated) continue;
                var delta = target.transform.position - transform.position;
                if (delta.x * Facing > 0f && delta.x * Facing < shotRange && Mathf.Abs(delta.y) < shotHeight)
                    target.Knockback(Facing, pushForce);
            }
        }

        float dt = Runner.DeltaTime;
        var oldPosition = transform.position;
        VerticalSpeed -= gravity * dt;
        var position = oldPosition + new Vector3(move * speed + PushX, VerticalSpeed, 0f) * dt;
        PushX = Mathf.MoveTowards(PushX, 0f, 18f * dt);
        if (VerticalSpeed <= 0f)
            foreach (var platform in FindObjectsByType<ArenaPlatform>(FindObjectsSortMode.None))
            {
                var b = platform.Bounds;
                if (position.x < b.min.x - 0.3f || position.x > b.max.x + 0.3f) continue;
                if (oldPosition.y - 1f >= b.max.y - 0.08f && position.y - 1f <= b.max.y)
                {
                    position.y = b.max.y + 1f;
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
        if (marker != null) marker.localPosition = new Vector3(Facing * 0.47f, 0.75f, -0.27f);
        if (beam != null)
        {
            beam.enabled = visible && !Flash.ExpiredOrNotRunning(Runner);
            beam.SetPosition(0, new Vector3(Facing * 0.6f, 0.25f, 0f));
            beam.SetPosition(1, new Vector3(Facing * shotRange, 0.25f, 0f));
        }
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
        Facing = position.x < 0f ? 1 : -1;
        Eliminated = false;
    }
}
