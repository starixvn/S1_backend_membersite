import {ErrorTips as Tips} from './errorTips';

export const InputBoxStatus = {
    'text':'text',
    'password':'password'
}

export const InputBox = (props)=>{
    const onChange = (e)=>{        
        if(props.onChange){
            let v = e.target.value;
            props.onChange(v, props.datacolumn)
        }
    }

    let elem = (
        <div className="textfield">
            <input type={props.type} className="textfield__input" placeholder={props.placeholder} value={props.value} onChange={onChange} autoComplete={props.autoComplete===true ? "on" : 'off'}/>
            <span className="textfield__bg" />
            {
                props.value && props.value.length > 0 ? (
                    <a className="btn btn-icon btn-form-togglepw" data-visibility={props.visibility} onClick={props.HandleVisible}>
                        <i className="icon icon--lg"></i>
                    </a>
                ) : null
            }
            <a className="btn btn-icon btn-form-status" onClick={props.HandleTips}>
                <i className="icon icon--lg"></i>
            </a>
        </div>
    );

    return props.columnTitle ? (
        <div className="form-group">
            <label className="form-title">{props.columnTitle}</label>
            <div className="form-content" data-status={props.dataStatus}>
                {elem}
                <Tips ErrorMsg={props.tipsMsg} />
            </div>
        </div>
    ) : elem
}