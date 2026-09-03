# Team Coding Standards

The purpose of this document is to provide the standards that the team must follow when coding in C#.

### Comments
- NO UNECESSARY comments are allowed. If your code is easy to read and understand do to correct naming and readability, do not add a comment please.


### Naming Conventions
Use names that are descriptive of the purpose/action of the Class, Methods, Variable.

- Classes & Methods: Pascal Case
- Attributes & Varibles: Camel Case

### Classes
- Regions
Utilizing regions allows you to collapse code blocks and keep your file clean and easy to read.

e.g. 
```
Public class Player
{
    #region Attributes

    public int health;
    public int speed;
    public int positionX;
    public int positionY;

    #endregion

    #region Constructors

    public Player()
    {
        this.health = 100;
        this.speed = 100;
        this.positionX = 0;
        this.positionY = 0;
    }

    public Player(int health, int speed, int positionX, int positionY)
    {
        this.health = health;
        this.speed = speed;
        this.positionX = positionX;
        this.positionY = positionY;
    }

    #endregion

    #region Player Movement

    public void Move()
    {
        // code here
    }

    public void Jump()
    {
        // code here
    }

    public void Crouch()
    {
        // code here
    }

    #endregion
}
```

### Variables
No use of var is allowed. Always use the correct type for your variable. This is to help ensure that the code is readable and understandable.

- Not to do:
```
var myVariable = 5;
```

- To do:
```
int myVariable = 5;
```

### Scope
- Functions & if statements should always have opening and closing brackets. 
- Each new scope will be on its own line.

Example of correct scope:
```
if (myVariable == 5)
{
    // code here
}
else if (myVariable == 20 && myVariable != 7)
{
    // code here
}
else
{
    // code here
}
```