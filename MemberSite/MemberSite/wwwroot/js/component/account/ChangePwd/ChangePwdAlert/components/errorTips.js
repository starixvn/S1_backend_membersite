export const ErrorTips = (props)=>{
    return(
        <div className="tooltip tooltip--bottom-right" data-open={props.ErrorMsg !== undefined && props.ErrorMsg !== null && props.ErrorMsg !== ''}>
            <span className="text">{props.ErrorMsg}</span>
        </div>
    )
}