class Modal extends React.Component {
    constructor(props) {
        super(props);
        this.state={show: false};
    }

    componentDidMount() {
        this.setState({show: true});
    }

    onClose() {
        this.setState({show: false}, function() {
            ReactDOM.unmountComponentAtNode(this.props.MyElem);
        }.bind(this));
    }

    render() {
        return (
            <div className="modal" data-open={this.state.show}>
                <div className="overlay" onClick={this.onClose.bind(this)}></div>
                <div className="modal__dialog">
                    <div className="modal__header">
                        <span className="text">{this.props.Header}</span>
                    </div>
                    <div className="modal__body">
                        {this.props.Body}
                    </div>
                    <div className="modal__footer">
                        <div className="btn-group">
                            <button className="btn btn-primary" onClick={this.onClose.bind(this)}>
                                <span className="text">{i18n.lbl_Close}</span>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        )
    }
}

let ShowModal = function(header, body) {
    let __elem = document.createElement('div');
    document.body.appendChild(__elem);
    ReactDOM.render(<Modal Header={header} Body={body} MyElem={__elem}/>, __elem);
};

export { ShowModal };