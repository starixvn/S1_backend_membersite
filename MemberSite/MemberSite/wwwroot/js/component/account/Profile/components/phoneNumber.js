import {InputBox} from './inputBox';
import {ErrorTips} from './errorTips';

export const PhoneNumber = (props)=>{
    return(
        <div className="form-group">
            <label className="form-title">{props.columnTitle}</label>
            <div className="form-content form-content--mobile" data-status={props.ReadOnly ? "readonly" : 'info'}>
                <div className="dropdown dropdown-default">
                    <a className="btn">
                        <span className="text">{props.areaCode}</span>
                    </a>
                </div>
                <InputBox 
                    dataColumn={props.dataColumn}
                    onChange={props.onChange}
                    regex={props.regex}
                    value={props.value}
                    ReadOnly={props.ReadOnly}
                />
                <ErrorTips ErrorMsg={props.ErrorMsg} />
            </div>
        </div>
    )
}