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
            <input type='text' className="textfield__input" placeholder={props.placeHolder ? props.placeHolder : ''} value={props.value} onChange={onChange}/>
            <span className="textfield__bg"></span>
        </div>
    );

    return props.columnTitle ? (
        <div className="form-group">
            <label className="form-title">{props.columnTitle}</label>
            <div className="form-content">
                {elem}
                <ErrorTips ErrorMsg={props.ErrorMsg} />
            </div>
        </div>
    ) : elem
}