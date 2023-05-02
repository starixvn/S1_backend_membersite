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