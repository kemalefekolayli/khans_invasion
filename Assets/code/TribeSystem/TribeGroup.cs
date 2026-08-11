using System.Collections.Generic;
using UnityEngine;

/// <summary>Independent civilian population group attached to a home state.</summary>
[RequireComponent(typeof(ProvinceWalker))]
public class TribeGroup : MonoBehaviour
{
    [Header("Recruitment")]
    [SerializeField] private float baseCharismaRequirement = 10f;
    [SerializeField] private float charismaPerPopulation = 0.2f;
    [SerializeField, Range(0f, 100f)] private float minimumCharismaRequirement = 10f;
    [SerializeField, Range(0f, 100f)] private float maximumCharismaRequirement = 80f;
    [SerializeField] private float followSpeed = 2.5f;
    [SerializeField] private Vector2 followOffset = new Vector2(-0.35f, -0.25f);

    public StateModel HomeState { get; private set; }
    [SerializeField] private float population;
    public float Population => population;
    public float RequiredCharisma => Mathf.Clamp(baseCharismaRequirement + population * charismaPerPopulation, minimumCharismaRequirement, maximumCharismaRequirement);
    public ProvinceWalker Walker { get; private set; }
    public Transform FollowTarget { get; private set; }
    public General RecruitingGeneral { get; private set; }

    private void Awake()
    {
        Walker = GetComponent<ProvinceWalker>();
    }

    private void OnEnable() => GameEvents.OnTurnEnded += OnTurnEnded;
    private void OnDisable() => GameEvents.OnTurnEnded -= OnTurnEnded;

    private void Update()
    {
        if (FollowTarget == null) return;
        transform.position = Vector3.MoveTowards(transform.position, FollowTarget.position + (Vector3)followOffset, followSpeed * Time.deltaTime);
    }

    public void Initialize(StateModel homeState, ProvinceModel startingProvince, float initialPopulation)
    {
        HomeState = homeState;
        population = Mathf.Max(0f, initialPopulation);
        if (Walker == null) Walker = GetComponent<ProvinceWalker>();
        Walker.SetProvince(startingProvince);
        transform.position = startingProvince.transform.position;
    }

    public bool TryRecruit(Transform target, CharismaSystem charisma)
    {
        if (target == null || charisma == null || charisma.Current < RequiredCharisma) return false;
        FollowTarget = target;
        RecruitingGeneral = target.GetComponent<General>();
        Walker?.CancelMovement();
        GameEvents.TribeRecruited(this, RecruitingGeneral);
        return true;
    }

    public float SetPopulation(float newPopulation)
    {
        population = Mathf.Max(0f, newPopulation);
        if (population <= 0f)
        {
            FollowTarget = null;
            RecruitingGeneral = null;
            gameObject.SetActive(false);
        }
        return population;
    }

    private void OnTurnEnded(int turnNumber)
    {
        if (FollowTarget != null || HomeState == null || Walker == null || Walker.CurrentProvince == null) return;

        List<ProvinceModel> candidates = new List<ProvinceModel>();
        foreach (ProvinceModel neighbor in Walker.CurrentProvince.neighbors)
        {
            if (neighbor != null && HomeState.provinceList.Contains(neighbor)) candidates.Add(neighbor);
        }

        if (candidates.Count > 0) Walker.MoveTo(candidates[Random.Range(0, candidates.Count)], 1.5f);
    }
}
