 let elem = null;
 let openCnt=0;

 class Loading extends React.Component {
    constructor(props) {
        super(props);
        this.state={show:false};
    }

    componentDidMount() {
        this.setState({show:true});
    }

    render() {
        return (
        <div className="app-loading" data-open={this.state.show}>
            <div className="preloader">
                <div className="preloader__spiner">
                    <div className="preloader__circles">
                        <div className="preloader__circle-1"></div>
                        <div className="preloader__circle-2"></div>
                    </div>
                </div>
            </div>
        </div>
        )
    }
}

let OnLoading = function() {
    ++openCnt;    
    if(elem == null) {
        elem = document.createElement('div');
        document.body.appendChild(elem);
        ReactDOM.render(<Loading/>, elem);
    }
};

let UnLoading = function() {
    --openCnt;
    if(openCnt == 0) {
        if(ReactDOM.unmountComponentAtNode(elem)){
            elem = null;
        }
    }
};

export { OnLoading, UnLoading }; 