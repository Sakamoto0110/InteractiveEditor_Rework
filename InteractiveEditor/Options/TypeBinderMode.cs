namespace InteractiveEditor.Options;

// How the Create builds the tree of a typed inspector (P1.7, P1.12). Automatic finds every member;
// Manual finds none, and the tree has only what is added by hand: members one by one (Add), buttons
// and displays.
public enum TypeBinderMode
{
    Automatic,
    Manual,
}
