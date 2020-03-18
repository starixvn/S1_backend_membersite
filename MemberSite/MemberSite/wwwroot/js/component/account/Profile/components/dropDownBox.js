import {ErrorTips} from './errorTips';

export class DropdownBox extends React.Component{
    constructor(props){
        super(props)
        this.state = {isOpen:false}
        this.ClickDropdown = this.ClickDropdown.bind(this);
        this.handleClickOutside = this.handleClickOutside.bind(this);
        this.SelectItem = this.SelectItem.bind(this);
        document.addEventListener('mousedown', this.handleClickOutside);
    }
    handleClickOutside(e) {
        if (this.wrapper && !this.wrapper.contains(e.target)) {
            this.setState({isOpen:false});
        }
    }
    SetOuterDivRef=(node)=>{
        this.wrapper = node;
    }
    ClickDropdown=()=>{
        this.setState({isOpen: !this.state.isOpen})
    }
    SelectItem=(v)=>{
        this.setState({isOpen:false})
        if(this.props.OnSelectItem !== undefined){
            this.props.OnSelectItem(v, this.props.DataColumn, this.props.BindingValue);
        }
    }
    render(){
        let rows = this.props.Options && this.props.Options.map(item=>{
            return(
                <div key={`dp-${item.value}`} className="dropdown__item" onClick={this.SelectItem.bind(null, item)} data-selected={this.props.SelectedItem !== undefined && this.props.SelectedItem !== null && this.props.SelectedItem.value === item.value}>
                    <span className="text">{item.displayText}</span>
                </div>
            );
        });

        let elem = (
            <div className="dropdown dropdown-default" ref={this.SetOuterDivRef} data-open={this.state.isOpen}>
                <a className="btn" onClick={this.ClickDropdown}>
                    <span className="text">{this.props.SelectedItem ? this.props.SelectedItem.displayText : ''}</span>
                    <i className="icon icon-caret"></i>
                </a>
                <div className="dropdown__container">
                    <div className="dropdown__list">
                        {rows}
                    </div>
                </div>
            </div>
        );

        return this.props.ColumnTitle ? (
            <div className="form-group">
                <label className="form-title">{this.props.ColumnTitle}</label>
                <div className="form-content" data-status="info">
                    {elem}
                    <ErrorTips ErrorMsg={this.props.ErrorMsg} />
                </div>
            </div>
        ) : elem
    }
}