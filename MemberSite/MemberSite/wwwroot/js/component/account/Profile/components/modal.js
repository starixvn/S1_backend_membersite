export const Modal = (props)=>{
    return(
        <div className="modal" data-open={props.isOpen}>
            <div className="overlay Js-DepositAlet"></div>
            <div className="modal__dialog">
                <div className="modal__header">
                    <span className="text">{props.title}</span>
                </div>
                <div className="modal__body">
                    {props.content}
                </div>
                <div className="modal__footer">
                    {props.btnBar}
                </div>
            </div>
        </div>
    )
}

export const ConfirmRealName = (props)=>{
    let btnBar = (
        <div className="btn-group">
            <button className="btn btn-primary" onClick={props.CloseModal}>
                <span className="text">{i18n.lbl_ReModify}</span>
            </button>
            <button className="btn btn-secondary" onClick={props.ConfirmProfile}>
                <span className="text">{i18n.lbl_ConfirmAndUpdate}</span>
            </button>
        </div>
    );

    let content = (
        <>
            <p><b>{props.AccountName}</b></p>
            <p>{i18n.msg_ConfirmRealNameAlert}</p>
        </>
    )

    return {title: i18n.lbl_Confirm, content: content, btnBar: btnBar}
}

export const ConfirmGoToDepositPage = (props)=>{
    let btnBar = (
        <div className="btn-group">
            <button className="btn btn-primary" onClick={props.CloseModal}>
                <span className="text">{i18n.lbl_Later}</span>
            </button>
            <a className="btn btn-secondary" href={`/${props.LangCode}/Cashier/Deposit`}>
                <span className="text">{i18n.lbl_DepositNow}</span>
            </a>
        </div>
    );

    let content = (
        <>
            <p>{i18n.msg_Updatesuccessful}</p>
            <p>{i18n.msg_GoToDepositPage}</p>
        </>
    )

    return {title: i18n.lbl_Success, content: content, btnBar: btnBar}
}

