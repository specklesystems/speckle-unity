using UnityEngine;

public class FirstPersonAudio : MonoBehaviour
{
    public FirstPersonMovement character;
    public GroundCheck groundCheck;

    [Header("Step")]
    public AudioSource stepAudio;
    public AudioSource runningAudio;
    [Tooltip("Minimum velocity for the step audio to play")]
    public float velocityThreshold = .01f;
    Vector2 lastCharacterPosition;
    Vector2 CurrentCharacterPosition => new Vector2(character.transform.position.x, character.transform.position.z);

    [Header("Landing")]
    public AudioSource landingAudio;
    public AudioClip[] landingSFX;

    [Header("Jump")]
    public Jump jump;
    public AudioSource jumpAudio;
    public AudioClip[] jumpSFX;

    [Header("Crouch")]
    public Crouch crouch;
    public AudioSource crouchStartAudio, crouchedAudio, crouchEndAudio;
    public AudioClip[] crouchStartSFX, crouchEndSFX;


    void Reset()
    {
        // Setup stuff.
        character = GetComponentInParent<FirstPersonMovement>();
        groundCheck = (transform.parent ?? transform).GetComponentInChildren<GroundCheck>();
        stepAudio = GetOrCreateAudioSource("Step audio");
        runningAudio = GetOrCreateAudioSource("Running audio");
        landingAudio = GetOrCreateAudioSource("Landing audio");

        // Jump audio.
        jump = GetComponentInParent<Jump>();
        if (jump)
            jumpAudio = GetOrCreateAudioSource("Jump audio");

        // Crouch audio.
        crouch = GetComponentInParent<Crouch>();
        if (crouch)
        {
            crouchStartAudio = GetOrCreateAudioSource("Crouch start audio");
            crouchStartAudio = GetOrCreateAudioSource("Crouched audio");
            crouchStartAudio = GetOrCreateAudioSource("Crouch end audio");
        }
    }

    void OnEnable()
    {
        // Subscribe to events.
        groundCheck.Grounded += PlayLandingAudio;
        if (jump)
            jump.Jumped += PlayJumpAudio;
        if (crouch)
        {
            crouch.CrouchStart += PlayCrouchStartAudio;
            crouch.CrouchEnd += PlayCrouchEndAudio;
        }
    }

    void OnDisable()
    {
        // Unsubscribe to events.
        groundCheck.Grounded -= PlayLandingAudio;
        if (jump)
            jump.Jumped -= PlayJumpAudio;
        if (crouch)
        {
            crouch.CrouchStart -= PlayCrouchStartAudio;
            crouch.CrouchEnd -= PlayCrouchEndAudio;
        }
    }

    void FixedUpdate()
    {
        // Play moving audio if the character is moving and on the ground.
        float velocity = Vector3.Distance(CurrentCharacterPosition, lastCharacterPosition);
        if (velocity >= velocityThreshold && groundCheck.isGrounded)
        {
            if (crouch && crouch.IsCrouched)
                UpdateMovingAudios(crouchedAudio, runningAudio, stepAudio);
            else if (character.IsRunning)
                UpdateMovingAudios(runningAudio, stepAudio, crouchedAudio);
            else
                UpdateMovingAudios(stepAudio, crouchedAudio, runningAudio);
        }
        else
            UpdateMovingAudios(null, stepAudio, crouchedAudio, runningAudio);
        lastCharacterPosition = CurrentCharacterPosition;
    }


    static void UpdateMovingAudios(AudioSource audioToPlay, params AudioSource[] audiosToPause)
    {
        // Play audio to play and pause the others.
        if (audioToPlay && !audioToPlay.isPlaying)
            audioToPlay.Play();
        foreach (var audio in audiosToPause)
            if (audio)
                audio.Pause();
    }

    void PlayLandingAudio() => PlayRandomClip(landingAudio, landingSFX);
    void PlayJumpAudio() => PlayRandomClip(jumpAudio, jumpSFX);
    void PlayCrouchStartAudio() => PlayRandomClip(crouchStartAudio, crouchStartSFX);
    void PlayCrouchEndAudio() => PlayRandomClip(crouchEndAudio, crouchEndSFX);


    #region Utility.
    AudioSource GetOrCreateAudioSource(string name)
    {
        // Try to get the audiosource.
        AudioSource result = System.Array.Find(GetComponentsInChildren<AudioSource>(), a => a.name == name);
        if (result)
            return result;

        // Audiosource does not exist, create it.
        result = new GameObject(name).AddComponent<AudioSource>();
        result.spatialBlend = 1;
        result.playOnAwake = false;
        result.transform.SetParent(transform, false);
        return result;
    }

    void PlayRandomClip(AudioSource audio, AudioClip[] clips)
    {
        if (!audio || clips.Length <= 0)
            return;

        // Get a random clip. If possible, make sure that it's not the same as the clip that is already on the audiosource.
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clips.Length > 1)
            while (clip == audio.clip)
                clip = clips[Random.Range(0, clips.Length)];

        // Play the clip.
        audio.clip = clip;
        audio.Play();
    }
    #endregion 
}
