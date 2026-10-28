using System;
using UnityEngine;

// Fusil à pompe de Nathan M4 (sous Red Met) : touche U, gâchette droite à la manette.
// 6 cartouches ; le tir est un AttackData (démarrage, coup de feu, récupération = pompe), compté en images
// à 60 FPS et figé pendant le hitstop. Gros recul de Nathan, douille éjectée, ralenti bref quand un tir
// tue plusieurs ennemis. Rechargement cartouche par cartouche (I, gâchette gauche, ou fusil vide),
// interrompu par l'esquive, un coup reçu ou un tir. Disponible seulement au stade M4 (NathanForm).
public class PlayerShotgun : MonoBehaviour
{
    [SerializeField] private AttackData attack;
    [SerializeField] private LayerMask targetLayers;
    [Tooltip("Bout du canon depuis le centre du corps, vers l'avant (calé sur le sprite de tir de M4).")]
    [SerializeField] private Vector2 muzzleOffset = new(1.1f, 0.35f);
    [SerializeField] private int magazine = 6;
    [Tooltip("Images (60 FPS) pour glisser une cartouche.")]
    [SerializeField] private int shellFrames = 24;
    [Tooltip("Vitesse du recul de Nathan au coup de feu.")]
    [SerializeField] private float recoilSpeed = 7f;
    [SerializeField] private AudioClip shellSound;

    private Rigidbody2D rb;
    private Health health;
    private PlayerController controller;
    private PlayerCombat combat;
    private PlayerDodge dodge;
    private bool fireRequested;
    private bool reloadRequested;
    private int frame;
    private int reloadFrame;
    private bool ejected;

    public bool Armed { get; private set; }
    public int Shells { get; private set; }
    public int Magazine => magazine;
    public bool IsFiring { get; private set; }
    public bool IsReloading { get; private set; }
    public bool IsBusy => IsFiring;
    public AttackData Attack => attack;

    public event Action<AttackData> Fired;
    public event Action ShellLoading;
    public event Action<int, int, bool> AmmoChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        controller = GetComponent<PlayerController>();
        combat = GetComponent<PlayerCombat>();
        dodge = GetComponent<PlayerDodge>();
        Shells = magazine;
    }

    public void SetArmed(bool value)
    {
        Armed = value;
        Shells = magazine;
        StopFiring();
        IsReloading = false;
        AmmoChanged?.Invoke(Shells, magazine, Armed);
    }

    private void Update()
    {
        if (!Armed || (health != null && health.IsDead))
            return;

        if (GameInput.ShotgunPressed)
            fireRequested = true;

        if (GameInput.ReloadPressed)
            reloadRequested = true;
    }

    private void FixedUpdate()
    {
        if (!Armed || (health != null && health.IsDead) || controller.IsFrozen)
            return;

        if (controller.IsInHitstun || (dodge != null && dodge.IsDodging))
        {
            StopFiring();
            IsReloading = false;
        }

        if (IsFiring)
        {
            StepFiring();
            return;
        }

        if (fireRequested)
        {
            fireRequested = false;

            if (Shells > 0 && CanStart())
            {
                StartFiring();
                return;
            }

            if (Shells == 0)
                reloadRequested = true;
        }

        if (reloadRequested)
        {
            reloadRequested = false;

            if (Shells < magazine && CanStart())
                StartReload();
        }

        if (IsReloading)
            StepReload();
    }

    private bool CanStart() =>
        controller.CanAct && (combat == null || !combat.IsAttacking) && (dodge == null || !dodge.IsDodging);

    private void StartFiring()
    {
        IsReloading = false;
        IsFiring = true;
        frame = 0;
        ejected = false;

        float input = controller.HorizontalInput;

        if (Mathf.Abs(input) > 0.01f)
            controller.SetFacing(input > 0f);

        controller.LockMovement(attack.TotalFrames / AttackData.FrameRate);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        Fired?.Invoke(attack);
    }

    private void StepFiring()
    {
        frame++;
        controller.LockMovement(2f / AttackData.FrameRate);

        if (frame == attack.startup + 1)
            Shoot();

        // Douille éjectée à la moitié de la pompe.
        if (!ejected && frame >= attack.startup + attack.active + attack.recovery / 3)
        {
            ejected = true;

            if (CombatVfx.Instance != null)
                CombatVfx.Instance.EjectShell(Muzzle() - new Vector2(controller.Facing * 0.6f, 0f), controller.Facing);
        }

        if (frame >= attack.TotalFrames)
            StopFiring();
    }

    private void Shoot()
    {
        Shells--;
        AmmoChanged?.Invoke(Shells, magazine, Armed);
        GameAudio.Play(attack.activeSound);
        Shotgun.Result result = Shotgun.Fire(gameObject, Muzzle(), new Vector2(controller.Facing, 0f), attack, targetLayers);

        // Recul : Nathan glisse en arrière (vitesse imposée, mouvement verrouillé).
        rb.linearVelocity = new Vector2(-controller.Facing * recoilSpeed, rb.linearVelocity.y);
        GameAudio.Play(attack.recoverySound, 0.8f);

        if (result.Kills >= 2 && CombatVfx.Instance != null)
            CombatVfx.Instance.MultiKill();
    }

    private void StopFiring()
    {
        if (!IsFiring)
            return;

        IsFiring = false;
        controller.UnlockMovement();
    }

    private void StartReload()
    {
        IsReloading = true;
        reloadFrame = 0;
        ShellLoading?.Invoke();
    }

    private void StepReload()
    {
        if (++reloadFrame < shellFrames)
            return;

        reloadFrame = 0;
        Shells++;
        GameAudio.Play(shellSound);
        AmmoChanged?.Invoke(Shells, magazine, Armed);

        if (Shells >= magazine)
            IsReloading = false;
        else
            ShellLoading?.Invoke();
    }

    private Vector2 Muzzle() => rb.position + new Vector2(muzzleOffset.x * controller.Facing, muzzleOffset.y);
}
