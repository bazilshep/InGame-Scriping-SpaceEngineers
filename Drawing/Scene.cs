using System.Collections.Generic;

namespace SpaceEngineersIngameScript.Drawing
{

    public class Scene
    {
        public List<Model> models;

        public Scene()
        {
            models = new List<Model>();
        }

        public void draw(Draw3 viewer)
        {
            foreach (Model m in models)
            {
                m.draw(viewer);
            }
        }
    }
    
}

