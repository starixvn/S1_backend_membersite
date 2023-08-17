import {ErrorTips} from './errorTips';

export const InputBox = (props)=>{
    const onChange = (e)=>{
        let v = e.target.value;
        let update = props.regex && v !== '' ? v.match(props.regex) !== null : true;
        
        if(update && props.onChange){
            props.onChange(v, props.dataColumn)
        }
    }

    let elem = (
        <div className="textfield">
            {
                props.ReadOnly ? 
                <input type='text' className="textfield__input" placeholder={props.placeHolder ? props.placeHolder : ''} value={props.value} onChange={onChange} readOnly/> :
                <input type='text' className="textfield__input" placeholder={props.placeHolder ? props.placeHolder : ''} value={props.value} onChange={onChange}/>
            }
            <span className="textfield__bg"></span>
            {
                props.ReadOnly ? <a className="btn btn-icon btn-form-status"><i className="icon icon--lg"></i></a> : null
            }
        </div>
    );

    return props.columnTitle ? (
        <div className="form-group">
            <label className="form-title">{props.columnTitle}</label>
            <div className="form-content" data-status={props.ReadOnly ? "readonly" : 'info'}>
                {elem}
                <ErrorTips ErrorMsg={props.ErrorMsg} />
            </div>
        </div>
    ) : elem
}