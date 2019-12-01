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
    _handleCheck=null;

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

    set handleCheck(value){
        this._handleCheck=value;
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
        this.setState({dropdown:!this.state.dropdown}, function() {
            if(this.state.dropdown){
                let object = document.getElementsByTagName('body')[0];
                $(object).on("mousedown", this.onHideDropdown.bind(this));
            }
        });

    }

    onHideDropdown(e) {
        this.setState({dropdown:false}, function() {
            let object = document.getElementsByTagName('body')[0];
            $(object).off();
        });
    }

    setValue(value) {
        this._dataField.Value=value;
        let object = document.getElementsByTagName('body')[0];
        $(object).off();        
        this.setState({dropdown:false});
    }

    handleChange(event) {
        this._dataField.Value=event.target.value;
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

        if(field.objectType==1) {
            input=<input type="text" className="textfield__input" placeholder={field.placeholder} onChange={this.handleChange.bind(this)} value={this._dataField.Value} onBlur={this.onBlur.bind(this)}/>;
        }

        if(field.objectType==2) {
            input=<input type={this.state.ShowPwd?"text":"password"} className="textfield__input" placeholder={field.placeholder} onChange={this.handleChange.bind(this)} value={this._dataField.Value} onBlur={this.onBlur.bind(this)}/>;
            ShowPwdBtn = <a className="btn btn-icon-sm btn-form-togglepw" data-visibility={this.state.ShowPwd} onClick={this.onShowPwd.bind(this)}><i className="icon"></i></a>;
        }

        let inputPanel = null;

        if(field.objectType==1 || field.objectType==2) {
            inputPanel = <div className="textfield">
                {input}
                <span className="textfield__bg"></span>
                {ShowPwdBtn}
                <a className="btn btn-icon-sm btn-form-status" onClick={field.status==1?this.onShowTooltip.bind(this):null}>
                    <i className="icon"></i>
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

                    dropdownList.push(<div className="dropdown__item" key={i} onClick={this.setValue.bind(this, dropdownItem.value)}>
                        <span className="text">{dropdownItem.text}</span>
                    </div>);
                }
            }

            inputPanel = <div className="dropdown dropdown-default" data-open={this.state.dropdown}>
                <a className="btn" onClick={this.onDropdown.bind(this)}>
                    <span className="text">{dropdownText}</span>
                    <i className="icon icon-caret"></i>
                    <div className="btn btn-icon-sm btn-form-status" onClick={field.status==1?this.onShowTooltip.bind(this):null}>
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
        userNameDataField.placeholder=i18n.lbl_UserName;
        userNameDataField.displayName=i18n.lbl_UserName;
        userNameDataField.tooltip="UserName";
        userNameDataField.status=1;
        userNameDataField.handleCheck = this.handleCheckUserName.bind(this);
        userNameDataField.Value="";
        formArr.push(userNameDataField);

        let userpwdDataField = new DataField("Userpwd");
        userpwdDataField.placeholder="Password";
        userpwdDataField.displayName="Password";
        userpwdDataField.tooltip="Password";
        userpwdDataField.status=1;
        userpwdDataField.objectType=2;
        userpwdDataField.handleCheck = this.handleCheckUserpwd.bind(this);        
        userpwdDataField.Value="";
        formArr.push(userpwdDataField);

        let currencyIdDataField = new DataField("CurrencyId");
        currencyIdDataField.displayName="Preferred Currency";
        currencyIdDataField.tooltip="Preferred Currency";
        currencyIdDataField.status=1;
        currencyIdDataField.objectType=3;
        currencyIdDataField.Value=defCurrencyId;
        currencyIdDataField.dropdownList=currencyList;
        formArr.push(currencyIdDataField);

        this.state = { formArr: formArr};
    }
    
    async handleCheckUserName(value) {
        let status = 1;
        let message = "tooltip";
        let regx = /^[a-zA-Z0-9]*$/;
        if(value!=null) {
            if(value.length == 0) {
                status = 2;
                message = "0";
            } else if(value.length < 6 || value.length > 18) {
                status = 2;
                message = "can't lo";
            } else if(!regx.test(value)) {
                status = 2;
                message = "仅限6-18 个字符，数字和字母组合(A-Z, a-z, 0-9)";
            } else {
                let res = await axios.post('/api/Site/ExistsUserName', {UserName: value});
                let data = res.data;
                if(data.errorCode==0 && data.data) {
                    status = 2;
                    message = value + " Exists";
                } else {
                    status = 3;
                    message = "";
                }
            }
        }

        return {status: status, message: message};
    }

    async handleCheckUserpwd(value) {
        let status = 1;
        let message = "tooltip";
        let regx = /^[a-zA-Z0-9@$*!%#?&]*$/;
        if(value!=null) {
            if(value.length == 0) {
                status = 2;
                message = "0";
            } else if(value.length < 6 || value.length > 18) {
                status = 2;
                message = "can't lo";
            } else if(!regx.test(value)) {
                status = 2;
                message = "仅限6-18 个字符，数字和字母组合(A-Z, a-z, 0-9) 或特殊字符@$!%*#?&";
            } else {
                status = 3;
                message = "";
            }
        }

        return {status: status, message: message};
    }

    handleSubmit(event) {
        let data = {};
        for(var i=0; i < this.state.formArr.length; i++){
            let dataField = this.state.formArr[i];
            data[dataField.name] = dataField.Value;
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
