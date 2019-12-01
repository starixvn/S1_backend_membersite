"use strict";

function _typeof(obj) {
    if (typeof Symbol === "function" && typeof Symbol.iterator === "symbol") {
        _typeof = function _typeof(obj) {
            return typeof obj;
        };
    } else {
        _typeof = function _typeof(obj) {
            return obj &&
                typeof Symbol === "function" &&
                obj.constructor === Symbol &&
                obj !== Symbol.prototype
                ? "symbol"
                : typeof obj;
        };
    }
    return _typeof(obj);
}

function _possibleConstructorReturn(self, call) {
    if (call && (_typeof(call) === "object" || typeof call === "function")) {
        return call;
    }
    return _assertThisInitialized(self);
}

function _getPrototypeOf(o) {
    _getPrototypeOf = Object.setPrototypeOf
        ? Object.getPrototypeOf
        : function _getPrototypeOf(o) {
            return o.__proto__ || Object.getPrototypeOf(o);
        };
    return _getPrototypeOf(o);
}

function _assertThisInitialized(self) {
    if (self === void 0) {
        throw new ReferenceError(
            "this hasn't been initialised - super() hasn't been called"
        );
    }
    return self;
}

function _inherits(subClass, superClass) {
    if (typeof superClass !== "function" && superClass !== null) {
        throw new TypeError("Super expression must either be null or a function");
    }
    subClass.prototype = Object.create(superClass && superClass.prototype, {
        constructor: { value: subClass, writable: true, configurable: true }
    });
    if (superClass) _setPrototypeOf(subClass, superClass);
}

function _setPrototypeOf(o, p) {
    _setPrototypeOf =
        Object.setPrototypeOf ||
        function _setPrototypeOf(o, p) {
            o.__proto__ = p;
            return o;
        };
    return _setPrototypeOf(o, p);
}

function _instanceof(left, right) {
    if (
        right != null &&
        typeof Symbol !== "undefined" &&
        right[Symbol.hasInstance]
    ) {
        return !!right[Symbol.hasInstance](left);
    } else {
        return left instanceof right;
    }
}

function _classCallCheck(instance, Constructor) {
    if (!_instanceof(instance, Constructor)) {
        throw new TypeError("Cannot call a class as a function");
    }
}

function _defineProperties(target, props) {
    for (var i = 0; i < props.length; i++) {
        var descriptor = props[i];
        descriptor.enumerable = descriptor.enumerable || false;
        descriptor.configurable = true;
        if ("value" in descriptor) descriptor.writable = true;
        Object.defineProperty(target, descriptor.key, descriptor);
    }
}

function _createClass(Constructor, protoProps, staticProps) {
    if (protoProps) _defineProperties(Constructor.prototype, protoProps);
    if (staticProps) _defineProperties(Constructor, staticProps);
    return Constructor;
}

function _defineProperty(obj, key, value) {
    if (key in obj) {
        Object.defineProperty(obj, key, {
            value: value,
            enumerable: true,
            configurable: true,
            writable: true
        });
    } else {
        obj[key] = value;
    }
    return obj;
}

var DataField =
    /*#__PURE__*/
    (function () {
        /*1. Text,2. Password,3. dropdownList*/
        function DataField(name) {
            _classCallCheck(this, DataField);

            _defineProperty(this, "_status", 1);

            _defineProperty(this, "_name", "");

            _defineProperty(this, "_tooltip", "");

            _defineProperty(this, "_displayName", "");

            _defineProperty(this, "_placeholder", "");

            _defineProperty(this, "_dataStatus", {
                "1": "info",
                "2": "error",
                "3": "valid"
            });

            _defineProperty(this, "_objectType", 1);

            _defineProperty(this, "_Value", null);

            _defineProperty(this, "_dropdownList", []);

            _defineProperty(this, "_handleCheck", null);

            this._name = name;
            this._objectType = 1;
        }

        _createClass(DataField, [
            {
                key: "onCheck",
                value: async function onCheck() {
                    if (this._handleCheck != null) {
                        var result = await this._handleCheck(this._Value);
                        this._status = result.status;
                        this._tooltip = result.message;
                        console.log(result.message);
                    }
                }
            },
            {
                key: "name",
                get: function get() {
                    return this._name;
                },
                set: function set(value) {
                    this._name = value;
                }
            },
            {
                key: "dataStatus",
                get: function get() {
                    return this._dataStatus[this._status.toString()];
                }
            },
            {
                key: "status",
                get: function get() {
                    return this._status;
                },
                set: function set(value) {
                    this._status = value;
                }
            },
            {
                key: "tooltip",
                get: function get() {
                    return this._tooltip;
                },
                set: function set(value) {
                    this._tooltip = value;
                }
            },
            {
                key: "displayName",
                get: function get() {
                    return this._displayName;
                },
                set: function set(value) {
                    this._displayName = value;
                }
            },
            {
                key: "placeholder",
                get: function get() {
                    return this._placeholder;
                },
                set: function set(value) {
                    this._placeholder = value;
                }
            },
            {
                key: "objectType",
                get: function get() {
                    return this._objectType;
                },
                set: function set(value) {
                    this._objectType = value;
                }
            },
            {
                key: "Value",
                get: function get() {
                    return this._Value;
                },
                set: function set(value) {
                    this._Value = value;
                }
            },
            {
                key: "dropdownList",
                get: function get() {
                    return this._dropdownList;
                },
                set: function set(value) {
                    this._dropdownList = value;
                }
            },
            {
                key: "handleCheck",
                set: function set(value) {
                    this._handleCheck = value;
                }
            }
        ]);

        return DataField;
    })();

var InputBox =
    /*#__PURE__*/
    (function (_React$Component) {
        _inherits(InputBox, _React$Component);

        function InputBox(props) {
            var _this;

            _classCallCheck(this, InputBox);

            _this = _possibleConstructorReturn(
                this,
                _getPrototypeOf(InputBox).call(this, props)
            );

            _defineProperty(_assertThisInitialized(_this), "_dataField", {});

            _this._dataField = _this.props.DataField;
            _this.state = {
                showTooltip: false,
                ShowPwd: false,
                dropdown: false
            };
            return _this;
        }

        _createClass(InputBox, [
            {
                key: "onShowTooltip",
                value: function onShowTooltip() {
                    this.setState({
                        showTooltip: !this.state.showTooltip
                    });
                }
            },
            {
                key: "onShowPwd",
                value: function onShowPwd() {
                    this.setState({
                        ShowPwd: !this.state.ShowPwd
                    });
                }
            },
            {
                key: "onDropdown",
                value: function onDropdown() {
                    this.setState({
                        dropdown: !this.state.dropdown
                    });
                    /*if(this.state.dropdown){
                    let object = document.getElementsByTagName('body')[0];
                    object.addEventListener("mousedown", this.onDropdown.bind(this));
                } else {
                    let object = document.getElementsByTagName('body')[0];
                    object.removeEventListener("mousedown", this.onDropdown.bind(this));
                }*/
                }
            },
            {
                key: "setValue",
                value: function setValue(value) {
                    this._dataField.Value = value;
                    this.setState({
                        dropdown: !this.state.dropdown
                    });
                }
            },
            {
                key: "handleChange",
                value: function handleChange(event) {
                    this._dataField.Value = event.target.value;
                    this.forceUpdate();
                }
            },
            {
                key: "onBlur",
                value: async function onBlur() {
                    await this._dataField.onCheck();
                    this.forceUpdate();
                }
            },
            {
                key: "render",
                value: function render() {
                    var input = null;
                    var ShowPwdBtn = null;
                    var field = this._dataField;

                    if (field.objectType == 1) {
                        input = React.createElement("input", {
                            type: "text",
                            className: "textfield__input",
                            placeholder: field.placeholder,
                            onChange: this.handleChange.bind(this),
                            value: this._dataField.Value,
                            onBlur: this.onBlur.bind(this)
                        });
                    }

                    if (field.objectType == 2) {
                        input = React.createElement("input", {
                            type: this.state.ShowPwd ? "text" : "password",
                            className: "textfield__input",
                            placeholder: field.placeholder,
                            onChange: this.handleChange.bind(this),
                            value: this._dataField.Value
                        });
                        ShowPwdBtn = React.createElement(
                            "a",
                            {
                                className: "btn btn-icon-sm btn-form-togglepw",
                                "data-visibility": this.state.ShowPwd,
                                onClick: this.onShowPwd.bind(this)
                            },
                            React.createElement("i", {
                                className: "icon"
                            })
                        );
                    }

                    var inputPanel = null;

                    if (field.objectType == 1 || field.objectType == 2) {
                        inputPanel = React.createElement(
                            "div",
                            {
                                className: "textfield"
                            },
                            input,
                            React.createElement("span", {
                                className: "textfield__bg"
                            }),
                            ShowPwdBtn,
                            React.createElement(
                                "a",
                                {
                                    className: "btn btn-icon-sm btn-form-status",
                                    onClick: this.onShowTooltip.bind(this)
                                },
                                React.createElement("i", {
                                    className: "icon"
                                })
                            )
                        );
                    } else if (field.objectType == 3) {
                        var dropdownList = [];
                        var dropdownText = null;

                        if (field.dropdownList.length > 0) {
                            for (var i = 0; i < field.dropdownList.length; i++) {
                                var dropdownItem = field.dropdownList[i];

                                if (field.Value == dropdownItem.value) {
                                    dropdownText = dropdownItem.text;
                                }

                                dropdownList.push(
                                    React.createElement(
                                        "div",
                                        {
                                            className: "dropdown__item",
                                            key: i,
                                            onClick: this.setValue.bind(this, dropdownItem.value)
                                        },
                                        React.createElement(
                                            "span",
                                            {
                                                className: "text"
                                            },
                                            dropdownItem.text
                                        )
                                    )
                                );
                            }
                        }

                        inputPanel = React.createElement(
                            "div",
                            {
                                className: "dropdown dropdown-default",
                                "data-open": this.state.dropdown
                            },
                            React.createElement(
                                "a",
                                {
                                    className: "btn",
                                    onClick: this.onDropdown.bind(this)
                                },
                                React.createElement(
                                    "span",
                                    {
                                        className: "text"
                                    },
                                    dropdownText
                                ),
                                React.createElement("i", {
                                    className: "icon icon-caret"
                                }),
                                React.createElement(
                                    "div",
                                    {
                                        className: "btn btn-icon-sm btn-form-status",
                                        onClick: this.onShowTooltip.bind(this)
                                    },
                                    React.createElement("i", {
                                        className: "icon"
                                    })
                                )
                            ),
                            React.createElement(
                                "div",
                                {
                                    className: "dropdown__container"
                                },
                                React.createElement(
                                    "div",
                                    {
                                        className: "dropdown__list"
                                    },
                                    dropdownList
                                )
                            )
                        );
                    }

                    return React.createElement(
                        "div",
                        {
                            className: "form-group"
                        },
                        React.createElement(
                            "label",
                            {
                                className: "form-title"
                            },
                            field.displayName
                        ),
                        React.createElement(
                            "div",
                            {
                                className: "form-content",
                                "data-status": field.dataStatus
                            },
                            inputPanel,
                            React.createElement(
                                "div",
                                {
                                    className: "tooltip tooltip--bottom-right",
                                    "data-open":
                                        (field.status != 3 &&
                                            this.state.showTooltip &&
                                            field.status == 1) ||
                                        field.status == 2
                                },
                                React.createElement(
                                    "span",
                                    {
                                        className: "text"
                                    },
                                    field.tooltip
                                )
                            )
                        )
                    );
                }
            }
        ]);

        return InputBox;
    })(React.Component);

var RegisteredForm =
    /*#__PURE__*/
    (function (_React$Component2) {
        _inherits(RegisteredForm, _React$Component2);

        //data-status="error" 1.info, 2.error,3.valid
        function RegisteredForm(props) {
            var _this2;

            _classCallCheck(this, RegisteredForm);

            _this2 = _possibleConstructorReturn(
                this,
                _getPrototypeOf(RegisteredForm).call(this, props)
            );
            var formArr = [];
            var userNameDataField = new DataField("UserName");
            userNameDataField.placeholder = "UserName";
            userNameDataField.displayName = "UserName";
            userNameDataField.tooltip = "UserName";
            userNameDataField.status = 1;
            userNameDataField.handleCheck = _this2.handleCheckUserName.bind(
                _assertThisInitialized(_this2)
            );
            userNameDataField.Value = "";
            formArr.push(userNameDataField);
            var userpwdDataField = new DataField("Userpwd");
            userpwdDataField.placeholder = "Password";
            userpwdDataField.displayName = "Password";
            userpwdDataField.tooltip = "Password";
            userpwdDataField.status = 1;
            userpwdDataField.objectType = 2;
            userpwdDataField.Value = "";
            formArr.push(userpwdDataField);
            var currencyIdDataField = new DataField("CurrencyId");
            currencyIdDataField.displayName = "Preferred Currency";
            currencyIdDataField.tooltip = "Preferred Currency";
            currencyIdDataField.status = 1;
            currencyIdDataField.objectType = 3;
            currencyIdDataField.Value = defCurrencyId;
            currencyIdDataField.dropdownList = currencyList;
            formArr.push(currencyIdDataField);
            _this2.state = {
                formArr: formArr
            };
            return _this2;
        }

        _createClass(RegisteredForm, [
            {
                key: "handleCheckUserName",
                value: async function handleCheckUserName(value) {
                    var status = 1;
                    var message = "";

                    if (value != null) {
                        if (value.length < 6 || value.length > 18) {
                            status = 2;
                            message = "can't lo";
                        } else {
                            var res = await axios.post("/api/Site/ExistsUserName", {
                                UserName: value
                            });
                            var data = res.data;

                            if (data.errorCode == 0 && data.data) {
                                status = 2;
                                message = "Exists";
                            }
                        }
                    }

                    return {
                        status: status,
                        message: message
                    };
                }
            },
            {
                key: "handleSubmit",
                value: function handleSubmit(event) {
                    var data = {};

                    for (var i = 0; i < this.state.formArr.length; i++) {
                        var dataField = this.state.formArr[i];
                        data[dataField.name] = dataField.Value;
                    }

                    data["UserLang"] = SiteLanguage;
                    axios
                        .post("/api/Site/Registered", data)
                        .then(function (response) {
                            var result = response.data;

                            if (result.errorCode == 0) {
                                alert(result.message);
                                location.reload();
                            } else {
                                alert(result.message);
                            }
                        })
                        .catch(function (error) {
                            alert(error);
                        });
                    event.preventDefault();
                }
                /*仅限8-10 个字符，数字和字母组合(A-Z, a-z, 0-9) 或特殊字符@$!%*#?&*/
            },
            {
                key: "render",
                value: function render() {
                    var Inputs = this.state.formArr.map(function (dataField) {
                        return React.createElement(InputBox, {
                            key: dataField.name,
                            DataField: dataField
                        });
                    });
                    return React.createElement(
                        "form",
                        {
                            className: "form",
                            onSubmit: this.handleSubmit.bind(this)
                        },
                        Inputs,
                        React.createElement(
                            "div",
                            {
                                className: "btn-group"
                            },
                            React.createElement(
                                "button",
                                {
                                    type: "submit",
                                    value: "submit"
                                },
                                "Completed Registration"
                            )
                        )
                    );
                }
            }
        ]);

        return RegisteredForm;
    })(React.Component);

ReactDOM.render(
    React.createElement(RegisteredForm, null),
    document.getElementById("Registered")
);
