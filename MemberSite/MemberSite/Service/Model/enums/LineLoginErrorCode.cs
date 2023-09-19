namespace MemberSite.Service.Model.enums
{
    public enum LineLoginErrorCode
    {
        Success = 0,

        MemberAccountAlreadyBinding = 1001,
        LineCallbackError = 1002,
        CodeInvaild = 1003,
        StateInvaid = 1004,

        NotFoundToken = 1005,
        FlowTypeInvaild = 1006,
        TokenExpired = 1007,

        IssueLineTokenFail = 2001,
        //GetLineTokenFail = 2002, //HttpCode!=200
        LineTokenInvaild = 2003,
        LineTokenNotFound = 2004,
        //GetLineUserProfileFail = 2005, //HttpCode!=200
        LineUserProfileInvaild = 2006,
        LineUserIdNotFound = 2007,
        LineGetUserIdException = 2009,

        RegisterAuthInvaild = 3001,
        RegisterLineProfileInvaild = 3002,
        LoginLineProfileInvaild = 3003,
        BindingAuthInvaild = 3004,
        BindingLineProfileInvaild = 3005,
        UndefineFlow = 3009,

        InsertLineProfileFail = 4001,
        InsertLineProfileException = 4009,

        IssueLineTokenException = 9001,
        CallBackException = 9002
    }
}