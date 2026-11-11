public interface Interactable
{
    bool CanInteract(PlayerInteractor player); //can interact or nah
    void Interact(PlayerInteractor player); //causes the itneraction
}
//for custoemrs etc