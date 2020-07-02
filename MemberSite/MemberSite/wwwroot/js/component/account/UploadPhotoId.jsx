import * as Loading from '../commJS/Loading.jsx';
(function ($, React, ReactDOM, i18n, axios, moment) {
    class UploadForm extends React.Component {
        constructor(props) {
            super(props);
            this.state={UploadDisabled: true, SubmitDisabled: false};
            this.filebox = React.createRef();
        }

        onCheckUpload(event) {
            let files = this.filebox.current.files;
            if(files.length >0 ) {
                this.setState({UploadDisabled: false});
            }
            else {
                this.setState({UploadDisabled: true});
            }
        }

        onUploadFile(event) {
            event.preventDefault();
            let files = this.filebox.current.files;
            if(files.length >0 ) {
                Loading.OnLoading();
                this.setState({UploadDisabled: false});
                let formData = new FormData();                
                formData.append("PhotoId", files[0]);
                axios.post('/api/AccountStrict/UploadCustPhotoId', formData, {
                    headers: {
                        'Content-Type': 'multipart/form-data'
                    }
                }).then(function (response) {
                    Loading.UnLoading();
                    let result = response.data;
                    if (result.errorCode == 0) {
                        //this.props.bankInfo.picFileName = files[0].
                        let fullPath = files[0].name;
                        var startIndex = (fullPath.indexOf('\\') >= 0 ? fullPath.lastIndexOf('\\') : fullPath.lastIndexOf('/'));
                        var filename = fullPath.substring(startIndex);
                        if (filename.indexOf('\\') === 0 || filename.indexOf('/') === 0) {
                            filename = filename.substring(1);
                        }

                        alert(i18n.lbl_UploadSuccess);
                        window.location.replace(window.location.href);

                    } else {
                        alert(result.message);
                    }
                }.bind(this)).catch(function (error) {
                    Loading.UnLoading();
                    alert(error);
                }.bind(this));          
            } else {
                this.setState({UploadDisabled: true});
            }
        }
   
        render() {
            return (
                <form className="form" onSubmit={this.onUploadFile.bind(this)}>
                    <div className="form-group">
                        <div className="form-content" data-status="">
                            <div className="textfield">
                                <input type="file" className="textfield__input" onChange={this.onCheckUpload.bind(this)} ref={this.filebox} accept="image/*"/>
                                <span className="textfield__bg"></span>
                            </div>
                            <button className="btn btn-primary" data-disabled={this.state.UploadDisabled} type="submit">
                                <div className="btn__inner">
                                    <span className="text">{i18n.lbl_UploadPhotoId}</span>
                                </div>
                            </button>
                        </div>
                    </div>
                </form>
            )
        }
    }

    ReactDOM.render(<UploadForm/>,document.getElementById('UploadForm'));
})(jQuery, React, ReactDOM, i18n, axios, moment);