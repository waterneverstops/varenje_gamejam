using UnityEngine;

public sealed class ToysManager : MonoBehaviour
{
    [SerializeField] private ToyPlace[] toyPlaces = new ToyPlace[3];
    [SerializeField] private GameObject solvedObject;
    [SerializeField] private bool hideSolvedObjectOnAwake = true;

    private bool solved;

    private void Awake()
    {
        if (hideSolvedObjectOnAwake && solvedObject != null)
        {
            solvedObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        SubscribePlaces();
        CheckSolved();
    }

    private void OnDisable()
    {
        UnsubscribePlaces();
    }

    private void OnPlaceStateChanged(ToyPlace place)
    {
        CheckSolved();
    }

    private void CheckSolved()
    {
        if (solved || !AreAllPlacesSolved())
        {
            return;
        }

        solved = true;

        if (solvedObject != null)
        {
            solvedObject.SetActive(true);
        }
    }

    private bool AreAllPlacesSolved()
    {
        if (toyPlaces == null || toyPlaces.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < toyPlaces.Length; i++)
        {
            ToyPlace place = toyPlaces[i];
            if (place == null || !place.HasMatchingToy)
            {
                return false;
            }
        }

        return true;
    }

    private void SubscribePlaces()
    {
        if (toyPlaces == null)
        {
            return;
        }

        for (int i = 0; i < toyPlaces.Length; i++)
        {
            ToyPlace place = toyPlaces[i];
            if (place != null)
            {
                place.StateChanged += OnPlaceStateChanged;
            }
        }
    }

    private void UnsubscribePlaces()
    {
        if (toyPlaces == null)
        {
            return;
        }

        for (int i = 0; i < toyPlaces.Length; i++)
        {
            ToyPlace place = toyPlaces[i];
            if (place != null)
            {
                place.StateChanged -= OnPlaceStateChanged;
            }
        }
    }
}
