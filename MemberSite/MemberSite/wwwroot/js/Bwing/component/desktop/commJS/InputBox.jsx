export class DataField {
    _status = 1;
    _name="";
    _tooltip="";
    _displayName="";
    _placeholder="";
    _dataStatus={"1":"info","2":"error","3":"valid","4":"readonly"};
    _objectType=1;/*1. Text,2. Password,3. dropdownList, 4. Email, 5.Number */
    _Value=null;
    _dropdownList=[];
    _handleCheck=null;
    _typeStr={1:"text",4:"email",5:"number", 6:"datetime-local"};
    _autoComplete = null;
    _labelCommit = null;

    constructor(name) {
        this._name=name;
        this._objectType=1;
    }
    
    get typeStr() {
        return this._typeStr;
    }

    get name() {
        return this._name;
    }
    
    set name(value) {
        this._name = value;
    }

    get dataStatus() {
      return this._dataStatus[this._status.toString()];
    }

    get status() {
        return this._status;
    }
  
    set status(value) {
        this._status = value;
    }

    get tooltip() {
        return this._tooltip;
    }
    
    set tooltip(value) {
        this._tooltip = value;
    }
    
    get displayName(){
        return this._displayName;
    }

    set displayName(value){
        this._displayName=value;
    }

    get placeholder(){
        return this._placeholder;
    }

    set placeholder(value){
        this._placeholder=value;
    }

    get objectType(){
        return this._objectType;
    }

    set objectType(value){
        this._objectType=value;
    }

    get Value(){
        return this._Value;
    }

    set Value(value){
        this._Value=value;
    }
    
    get dropdownList(){
        return this._dropdownList;
    }

    set dropdownList(value){
        this._dropdownList=value;
    }

    set handleCheck(value){
        this._handleCheck=value;
    }

    set AutoComplete(value){
        this._autoComplete=value;
    }

    get LabelCommit(){
        return this._labelCommit;
    }

    set LabelCommit(value){
        this._labelCommit=value;
    }

    

    async onCheck() {
        if(this._handleCheck != null)
        {
            let result = await this._handleCheck(this._Value);
            this._status = result.status;
            this._tooltip = result.message;
        }
    }
}

export class InputBox extends React.Component {
    _dataField={};
    
    constructor(props) {
        super(props);
        this._dataField=this.props.DataField;
        this.state = { showTooltip: false, ShowPwd: false, dropdown: false};
        this.Dropdown = React.createRef();
    }

    onShowTooltip() {
        this.setState({showTooltip:!this.state.showTooltip, dropdown: false});
    }
    
    onShowPwd() {
        this.setState({ShowPwd:!this.state.ShowPwd});
    }

    onDropdown(event) {
        let node = event.target;
        let parentNode = node.parentNode;
        if(node.className != "btn btn-icon btn-form-status" && (parentNode!=undefined && parentNode.className != "btn btn-icon btn-form-status")) {
            let dropdownVal = !this.state.dropdown;
            let $target = $(this.Dropdown.current);

            //console.log(typeof($target.attr("data-open")));
            //console.log($target.attr("data-open"));
            if(typeof($target.attr("data-open"))!=dropdownVal) {
                dropdownVal = true;
            }

            this.setState({dropdown: dropdownVal});
        }
    }

    setValue(value) {
        this._dataField.Value=value;
        this.setState({dropdown:false});
    }

    handleChange(event) {
        if(this._dataField.name == "Amount") {
            var re = /^\+?[1-9][0-9]*$/;
            if(!isNaN(event.target.value) && re.test(event.target.value)) {
                if (IsConvertRequired) {
                    this._dataField.Value = parseFloat(event.target.value) * 1000;
                } else {
                    this._dataField.Value = event.target.value;
                }
            } else if(event.target.value=="") {
                this._dataField.Value = event.target.value;
            }
        } else {
            this._dataField.Value = event.target.value;
        }
        this.forceUpdate();
    }

    async onBlur() {
        await this._dataField.onCheck();
        this.forceUpdate();
    }

    render() {
        let input = null;
        let ShowPwdBtn = null;
        let field = this._dataField;

        if(field.objectType!=3) {
            let thisType=field.typeStr[field.objectType];
            if(field.objectType==2) {
                thisType = this.state.ShowPwd ? "text":"password";
                let __Value = this._dataField.Value;
                ShowPwdBtn = (__Value&&__Value.length>0)||this.state.ShowPwd ? <a className="btn btn-icon btn-form-togglepw" data-visibility={!this.state.ShowPwd} onClick={this.onShowPwd.bind(this)}><i className="icon icon--lg"></i></a>:null;
            }
            
            let dispValue = field.Value;

            if (field.name == "Amount" && !isNaN(dispValue) && IsConvertRequired) {
                dispValue = parseFloat(dispValue) / 1000;
            }

            input=<input type={thisType} className="textfield__input" placeholder={field.placeholder} onChange={field.status != 4 ? this.handleChange.bind(this) : null} readOnly={field.status == 4} value={dispValue} onBlur={this.onBlur.bind(this)} pattern={field.name == "Amount" ? "[0-9]*": null} autoComplete={this._autoComplete}/>;
        }

        let inputPanel = null;

        if(field.objectType != 3) {
            inputPanel = <div className="textfield">
                {input}
                {field.name == "Amount" && IsConvertRequired ? <span className="text">('000)</span> : null}
                <span className="textfield__bg"></span>
                {ShowPwdBtn}
                <a className="btn btn-icon btn-form-status" onClick={field.status==1?this.onShowTooltip.bind(this):null}>
                    <i className="icon icon--lg"></i>
                </a>
            </div>;
        } else if (field.objectType==3){
            let dropdownList=[];
            let dropdownText=null;
            if(field.dropdownList.length>0) {
                for(var i=0; i < field.dropdownList.length; i++){
                    let dropdownItem=field.dropdownList[i];
                    if(field.Value==dropdownItem.value) {
                        dropdownText=dropdownItem.text;
                    }

                    dropdownList.push(<div className="dropdown__item" key={i} onClick={this.setValue.bind(this, dropdownItem.value)} data-selected={dropdownItem.value==field.Value}>
                        <span className="text">{dropdownItem.text}</span>
                    </div>);
                }
            }

            inputPanel = <div className="dropdown dropdown-default" data-open={this.state.dropdown} ref={this.Dropdown}>
                <a className="btn" onClick={this.onDropdown.bind(this)}>
                    <span className="text">{dropdownText}</span>
                    <i className="icon icon-caret"></i>
                    <div className="btn btn-icon btn-form-status" onClick={field.status==1?this.onShowTooltip.bind(this):null}>
                        <i className="icon icon--lg"></i>
                    </div>
                </a>
                <div className="dropdown__container">
                    <div className="dropdown__list">
                        {dropdownList}
                    </div>
                </div>
            </div>;
        }

        return (
        <div className="form-group">
            <label className="form-title"><span className={`text${this.props.titleClassName ? ` ${this.props.titleClassName}` : ''}`}>{field.displayName}</span>{field.LabelCommit}</label>
            <div className="form-content" data-status={field.dataStatus}>
                {inputPanel}
                <div className="tooltip tooltip--bottom-right" data-open={(field.status!=3 && field.status!=4 && (this.state.showTooltip && field.status==1))||field.status==2}>
                    <span className="text">{field.tooltip}</span>
                </div>
            </div>
        </div>
        )
    }
}