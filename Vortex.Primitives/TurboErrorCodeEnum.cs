namespace Vortex.Primitives;

public enum TurboErrorCodeEnum
{
    Unknown = 0,
    AvatarNotFound,
    PlayerNotFound,
    RoomNotFound,
    ModelNotFound,
    MapNotLoaded,
    TileOutOfBounds,
    FloorItemNotFound,
    WallItemNotFound,
    FurnitureDefinitionNotFound,
    InvalidLogic,
    InvalidWired,
    InvalidFurnitureProductType,
    InvalidSession,
    InvalidMoveTarget,
    NoPermissionToPlaceFurni,
    NoPermissionToManipulateFurni,
    PetNotFound,
    PetFoodNotFound,
    NoPermissionToManipulatePet,
}

public static class ErrorCodeExtensions
{
    public static string ToDefaultMessage(this TurboErrorCodeEnum code) =>
        code switch
        {
            TurboErrorCodeEnum.AvatarNotFound => "The specified avatar could not be found.",
            TurboErrorCodeEnum.PlayerNotFound => "The specified player could not be found.",
            TurboErrorCodeEnum.RoomNotFound => "The specified room could not be found.",
            TurboErrorCodeEnum.ModelNotFound => "The room model could not be found.",
            TurboErrorCodeEnum.MapNotLoaded => "The room map is not loaded.",
            TurboErrorCodeEnum.TileOutOfBounds => "The tile index is out of bounds.",
            TurboErrorCodeEnum.FloorItemNotFound => "The specified floor item could not be found.",
            TurboErrorCodeEnum.WallItemNotFound => "The specified wall item could not be found.",
            TurboErrorCodeEnum.FurnitureDefinitionNotFound =>
                "The specified furniture definition could not be found.",
            TurboErrorCodeEnum.InvalidLogic => "The logic is not valid.",
            TurboErrorCodeEnum.InvalidWired => "The wired definition is not valid.",
            TurboErrorCodeEnum.InvalidFurnitureProductType =>
                "The furniture product type is invalid.",
            TurboErrorCodeEnum.InvalidSession => "The session is invalid.",
            TurboErrorCodeEnum.InvalidMoveTarget => "The move target is invalid.",
            TurboErrorCodeEnum.NoPermissionToPlaceFurni =>
                "You do not have permission to place furniture.",
            TurboErrorCodeEnum.NoPermissionToManipulateFurni =>
                "You do not have permission to manipulate furniture.",
            TurboErrorCodeEnum.PetNotFound => "The specified pet could not be found.",
            TurboErrorCodeEnum.PetFoodNotFound => "The specified pet food could not be found.",
            TurboErrorCodeEnum.NoPermissionToManipulatePet =>
                "You do not have permission to manipulate this pet.",
            _ => "An unknown error occurred.",
        };
}
