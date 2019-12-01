class DataField {
    _status = 1;
    _name="";
    _tooltip="";
    _displayName="";
    _placeholder="";
    _dataStatus={"1":"info","2":"error","3":"valid"};
    _objectType=1;/*1. Text,2. Password,3. dropdownList*/
    _Value=null;
    _dropdownList=[];

    constructor(name) {
        this._name=name;
        this._objectType=1;
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
}

class InputBox extends React.Component {
    _dataField={};
    
    constructor(props) {
        super(props);
        this._dataField=this.props.DataField;
        this.state = { showTooltip: false, ShowPwd: false, dropdown: false};
    }

    onShowTooltip() {
        this.setState({showTooltip:!this.state.showTooltip});
    }
    
    onShowPwd() {
        this.setState({ShowPwd:!this.state.ShowPwd});
    }

    onDropdown() {
        this.setState({dropdown:!this.state.dropdown});
        /*if(this.state.dropdown){
            let object = document.getElementsByTagName('body')[0];
            object.addEventListener("mousedown", this.onDropdown.bind(this));
        } else {
            let object = document.getElementsByTagName('body')[0];
            object.removeEventListener("mousedown", this.onDropdown.bind(this));
        }*/
    }

    setValue(value) {
        this._dataField.value=value;
        this.setState({dropdown:!this.state.dropdown});
    }

    handleChange(event) {
        this._dataField.value=event.target.value;
        this.forceUpdate();
    }

    render() {
        let input = null;
        let ShowPwdBtn = null;
        let field = this._dataField;

        if(field.objectType==1) {
            input=<input type="text" className="textfield__input" placeholder={field.placeholder} onChange={this.handleChange.bind(this)}/>;
        }

        if(field.objectType==2) {
            input=<input type={this.state.ShowPwd?"text":"password"} className="textfield__input" placeholder={field.placeholder} onChange={this.handleChange.bind(this)}/>;
            ShowPwdBtn = <a className="btn btn-icon-sm btn-form-togglepw" data-visibility={this.state.ShowPwd} onClick={this.onShowPwd.bind(this)}><i className="icon"></i></a>;
        }

        let inputPanel = null;

        if(field.objectType==1 || field.objectType==2) {
            inputPanel = <div className="textfield">
                {input}
                <span className="textfield__bg"></span>
                {ShowPwdBtn}
                <a className="btn btn-icon-sm btn-form-status" onClick={this.onShowTooltip.bind(this)}>
                    <i className="icon"></i>
                </a>
            </div>;
        } else if (field.objectType==3){
            let dropdownList=[];
            let dropdownText=null;
            if(field.dropdownList.length>0) {
                for(var i=0; i < field.dropdownList.length; i++){
                    let dropdownItem=field.dropdownList[i];
                    if(field.value==dropdownItem.value) {
                        dropdownText=dropdownItem.text;
                    }

                    dropdownList.push(<div className="dropdown__item" key={i} onClick={this.setValue.bind(this, dropdownItem.value)}>
                        <span className="text">{dropdownItem.text}</span>
                    </div>);
                }
            }

            inputPanel = <div className="dropdown dropdown-default" data-open={this.state.dropdown}>
                <a className="btn" onClick={this.onDropdown.bind(this)}>
                    <span className="text">{dropdownText}</span>
                    <i className="icon icon-caret"></i>
                    <div className="btn btn-icon-sm btn-form-status" onClick={this.onShowTooltip.bind(this)}>
                        <i className="icon"></i>
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
            <label className="form-title">{field.displayName}</label>
            <div className="form-content" data-status={field.dataStatus}>
                {inputPanel}
                <div className="tooltip tooltip--bottom-right" data-open={(field.status!=3 && (this.state.showTooltip && field.status==1))||field.status==2}>
                    <span className="text">{field.tooltip}</span>
                </div>
            </div>
        </div>
        )
    }
}

class RegisteredForm extends React.Component {
    //data-status="error" 1.info, 2.error,3.valid
    constructor(props) {
        super(props);
        let formArr=[];

        let userNameDataField = new DataField("UserName");
        userNameDataField.placeholder="UserName";
        userNameDataField.displayName="UserName";
        userNameDataField.tooltip="UserName";
        userNameDataField.status=1;
        formArr.push(userNameDataField);

        let UserpwdDataField = new DataField("Userpwd");
        UserpwdDataField.placeholder="Password";
        UserpwdDataField.displayName="Password";
        UserpwdDataField.tooltip="Password";
        UserpwdDataField.status=1;
        UserpwdDataField.objectType=2;
        formArr.push(UserpwdDataField);

        let CurrencyIdDataField = new DataField("CurrencyId");
        CurrencyIdDataField.displayName="Preferred Currency";
        CurrencyIdDataField.tooltip="Preferred Currency";
        CurrencyIdDataField.status=1;
        CurrencyIdDataField.objectType=3;
        CurrencyIdDataField.value=defCurrencyId;
        CurrencyIdDataField.dropdownList=currencyList;
        formArr.push(CurrencyIdDataField);

        this.state = { formArr: formArr};
    }
    
    handleSubmit(event) {
        let data = {};
        for(var i=0; i < this.state.formArr.length; i++){
            let dataField = this.state.formArr[i];
            data[dataField.name] = dataField.value;
        }

        data["UserLang"] = SiteLanguage;
        
        axios.post('/api/Site/Registered', data)
        .then(function (response) {
            let result = response.data;
            if (result.errorCode == 0) {
                alert(result.message);
                location.reload();
            } else {
                alert(result.message);
            }
        }).catch(function (error) {
            alert(error);
        });
        event.preventDefault();
    }

    /*仅限8-10 个字符，数字和字母组合(A-Z, a-z, 0-9) 或特殊字符@$!%*#?&*/
    render() {
        let Inputs=this.state.formArr.map(dataField => <InputBox key={dataField.name} DataField={dataField}/>);

        return (
            <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                {Inputs}
                <div className="btn-group">
                    <button type="submit" value="submit">Completed Registration</button>
                </div>
            </form>
        )
    }
}

ReactDOM.render(<RegisteredForm/>,document.getElementById('Registered'));
